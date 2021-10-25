using Entities.Cadastre;
using Entities.Permits;
using Entities.Scraping;
using Repository.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PermitsScraper.Services
{
    public class PermitsImportService : IPermitsImportService
    {
        private readonly IEnterprisesRepository _enterprisesRepository;
        private readonly IForestriesRepository _forestriesRepository;
        private readonly IBlocksRepository _blocksRepository;
        private readonly ISitesRepository _sitesRepository;
        private readonly IPermitsRepository _permitsRepository;
        private readonly Dictionary<string, Enterprise> _enterprisesCache;

        public PermitsImportService(IEnterprisesRepository enterprisesRepository, IForestriesRepository forestriesRepository, IBlocksRepository blocksRepository, ISitesRepository sitesRepository, IPermitsRepository permitsRepository)
        {
            _enterprisesRepository = enterprisesRepository;
            _forestriesRepository = forestriesRepository;
            _blocksRepository = blocksRepository;
            _sitesRepository = sitesRepository;
            _permitsRepository = permitsRepository;
            _enterprisesCache = new Dictionary<string, Enterprise>();
        }

        public void Import(PermitsImportArgs args)
        {
            // Get enterprise id and mu_kod by enteprise name | name: 'Kazlų Rūdos miškų ūredija' -> id: 7, mu_kod: 52
            var enterprise = GetEnterprise(args.Enterprise);
            if (enterprise == null)
            {
                Console.WriteLine($"Enterprise not found: {args.Enterprise}");
                return;
            }
            // Get forestry id and gir_kod by mu_kod and forestry name | mu_kod: 52, name: 'Agurkiškės girininkija' ->  id: 83, gir_kod: 3
            var forestry = GetForestry(enterprise.Code, args.Forestry);
            if (forestry == null)
            {
                Console.WriteLine($"Forestry not found: {args.Forestry} | Enterprise: {args.Enterprise}");
                return;
            }

            foreach (var permitGroupByNumber in args.ScrapedPermits.GroupBy(p => p.Number))
            {
                var generalPermitInfo = permitGroupByNumber.FirstOrDefault();
                var existingPermit = _permitsRepository.GetFullPermit(generalPermitInfo.Number);

                var newPermit = new Permit
                {
                    PermitNumber = generalPermitInfo.Number,
                    Region = generalPermitInfo.Region,
                    District = generalPermitInfo.District,
                    OwnershipForm = generalPermitInfo.OwnershipForm,
                    CadastralEnterpriseId = enterprise.Id,
                    CadastralForestryId = forestry.Id,
                    CadastralLocation = generalPermitInfo.CadastralLocation,
                    CadastralBlock = generalPermitInfo.CadastralBlock,
                    CadastralNumber = generalPermitInfo.CadastralNumber,
                    CuttingType = generalPermitInfo.CuttingType,
                    ValidFrom = DateTime.Parse(generalPermitInfo.ValidFrom),
                    ValidTo = DateTime.Parse(generalPermitInfo.ValidTo),
                };

                //Find Differences
                if (existingPermit != null)
                {
                    UpdatePermit(existingPermit, newPermit, enterprise, forestry);
                    newPermit.Id = existingPermit.Id;
                }
                else
                {
                    newPermit.Id = _permitsRepository.InsertPermit(newPermit);
                }

                var unhandledExistingPermitBlocks = new List<PermitBlock>();
                if (existingPermit?.PermitBlocks?.Count > 0)
                    unhandledExistingPermitBlocks.AddRange(existingPermit.PermitBlocks);

                var newPermitBlocks = new List<PermitBlock>();
                foreach(var permitGroupByBlock in permitGroupByNumber.GroupBy(pg => pg.Block))
                {
                    var defaultPermitBlockInfo = permitGroupByBlock.FirstOrDefault();
                    var cadastralBlockId = _blocksRepository.GetBlockId(enterprise.Code, forestry.Code, defaultPermitBlockInfo.Block);
                    if (!cadastralBlockId.HasValue)
                    {
                        Console.WriteLine("Block not found");
                        continue;
                    }
                    var permitBlock = new PermitBlock
                    {
                        PermitId = newPermit.Id,
                        CadastralBlockId = cadastralBlockId.Value,
                    };

                    var existingPermitBlock = existingPermit?.PermitBlocks?.Find(pb => pb.CadastralBlockId == cadastralBlockId.Value);
                    if (existingPermitBlock != null)
                    {
                        unhandledExistingPermitBlocks.Remove(existingPermitBlock);
                        permitBlock.Id = existingPermitBlock.Id;
                    }
                    else
                    {
                        permitBlock.Id = _permitsRepository.InsertPermitBlock(permitBlock);
                        if (existingPermit != null)
                            _permitsRepository.InsertPermitHistory(newPermit.Id, $"Naujas kvartalas: {defaultPermitBlockInfo.Block}");
                    }

                    permitBlock.PermitSites = GetNewPermitSites(permitGroupByBlock, enterprise, forestry, permitBlock);
                    newPermitBlocks.Add(permitBlock);

                    var unhandledExistingPermitSites = new List<PermitSite>();
                    if (existingPermitBlock?.PermitSites?.Count > 0)
                        unhandledExistingPermitSites.AddRange(existingPermitBlock.PermitSites);

                    foreach (var newPermitSite in permitBlock.PermitSites)
                    {
                        if (unhandledExistingPermitSites.Count > 0)
                        {
                            var existingPermitSite = unhandledExistingPermitSites.Find(ps => ps.CadastralSiteId != null && ps.CadastralSiteId == newPermitSite.CadastralSiteId);
                            if (existingPermitSite != null)
                            {
                                unhandledExistingPermitSites.Remove(existingPermitSite);
                                UpdatePermitSite(existingPermitSite, newPermitSite, existingPermit.Id);
                            }
                            else
                            {
                                var existingUnmappedPermitSite = unhandledExistingPermitSites.Find(ps => ps.CadastralSiteId == null && newPermitSite.CadastralSiteId == null && ps.SiteCodes[0] == newPermitSite.SiteCodes[0]);
                                if (existingUnmappedPermitSite != null)
                                {
                                    unhandledExistingPermitSites.Remove(existingPermitSite);
                                    UpdatePermitSite(existingUnmappedPermitSite, newPermitSite, existingPermit.Id);
                                }
                                else
                                {
                                    _permitsRepository.InsertPermitSite(newPermitSite);
                                    _permitsRepository.InsertPermitHistory(existingPermit.Id, newPermitSite.SiteCodes.Count > 1 ? $"Nauji sklypai: {string.Join(", ", newPermitSite.SiteCodes)}" : $"Naujas sklypas: {newPermitSite.SiteCodes[0]}");
                                }
                            }
                        }
                        else
                        {
                            _permitsRepository.InsertPermitSite(newPermitSite);
                            if (existingPermit != null)
                                _permitsRepository.InsertPermitHistory(newPermit.Id, newPermitSite.SiteCodes.Count > 1 ? $"Nauji sklypai: {string.Join(", ", newPermitSite.SiteCodes)}" : $"Naujas sklypas: {newPermitSite.SiteCodes[0]}");
                        }
                    }

                    //Delete removed permit sites
                    if (unhandledExistingPermitSites.Count > 0)
                    {
                        _permitsRepository.DeletePermitSites(unhandledExistingPermitSites.Select(ps => ps.Id).ToList());
                        foreach (var unhandledExistingPermitSite in unhandledExistingPermitSites)
                            _permitsRepository.InsertPermitHistory(existingPermit.Id, unhandledExistingPermitSite.SiteCodes.Count > 1 ? $"Ištrinti sklypai: {string.Join(", ", unhandledExistingPermitSite.SiteCodes)}" : $"Ištrintas sklypas: {unhandledExistingPermitSite.SiteCodes[0]}");
                    }

                    var blockHasUnmappedSite = permitBlock.PermitSites.Any(p => !p.CadastralSiteId.HasValue);
                    if (existingPermitBlock != null && existingPermitBlock.HasUnmappedSites != blockHasUnmappedSite)
                    {
                        existingPermitBlock.HasUnmappedSites = blockHasUnmappedSite;
                        _permitsRepository.UpdatePermitBlock(existingPermitBlock);
                    }
                }

                //Delete removed permit blocks
                if (unhandledExistingPermitBlocks.Count > 0)
                {
                    foreach (var unhandledExistingPermitBlock in unhandledExistingPermitBlocks)
                    {
                        _permitsRepository.DeletePermitSites(unhandledExistingPermitBlock.Id);
                        _permitsRepository.DeletePermitBlock(unhandledExistingPermitBlock.Id);
                        _permitsRepository.InsertPermitHistory(existingPermit.Id, $"Ištrintas sklypas: {_blocksRepository.GetBlockNumberById(unhandledExistingPermitBlock.CadastralBlockId)}");
                    }
                }
            }
        }

        private Enterprise GetEnterprise(string enterpriseName)
        {
            Enterprise foundEnterprise = null;
            if (_enterprisesCache.TryGetValue(enterpriseName, out Enterprise cachedEnterprise))
                foundEnterprise = cachedEnterprise;

            if (foundEnterprise == null)
            {
                var enterprise = _enterprisesRepository.GetEnterpriseByFullName(enterpriseName);
                if (enterprise != null)
                {
                    foundEnterprise = enterprise;
                    _enterprisesCache.Add(enterpriseName, foundEnterprise);
                }
                else
                {
                    var enterpriseNameFragments = enterpriseName.Split(" ").ToList();
                    foreach (var primaryNameFragment in enterpriseNameFragments)
                    {
                        var enterprises = _enterprisesRepository.GetEnterprisesByNameFragment(primaryNameFragment);
                        if (enterprises.Count == 1)
                        {
                            foundEnterprise = enterprises[0];
                            _enterprisesCache.Add(enterpriseName, foundEnterprise);
                            break;
                        }
                        else if (enterprises.Count > 1)
                        {
                            var secondaryNameFragments = new List<string>(enterpriseNameFragments);
                            secondaryNameFragments.Remove(primaryNameFragment);
                            foundEnterprise = enterprises.Find(e => secondaryNameFragments.Any(fragment => e.Name.Contains(fragment))) ?? enterprises.Find(e => e.Name.Split(" ").Length == 1);

                            _enterprisesCache.Add(enterpriseName, foundEnterprise);
                            break;
                        }
                    }

                    if (foundEnterprise != null)
                    {
                        foundEnterprise.FullName = enterpriseName;
                        _enterprisesRepository.UpdateFullName(foundEnterprise.Id, enterpriseName);
                    }
                }
            }

            return foundEnterprise;
        }

        private Forestry GetForestry(int enterpriseCode, string forestryName)
        {
            Forestry foundForestry = null;
            var forestry = _forestriesRepository.GetForestryByFullName(enterpriseCode, forestryName);
            if (forestry != null)
            {
                foundForestry = forestry;
            }
            else
            {
                var forestryNameFragments = forestryName.Split(" ").ToList();
                foreach (var primaryNameFragment in forestryNameFragments)
                {
                    var forestries = _forestriesRepository.GetForestries(enterpriseCode, primaryNameFragment);

                    if (forestries.Count == 1)
                    {
                        foundForestry = forestries[0];
                        break;
                    }
                    else if (forestries.Count > 1)
                    {
                        var secondaryNameFragments = new List<string>(forestryNameFragments);
                        secondaryNameFragments.Remove(primaryNameFragment);
                        foundForestry = forestries.Find(f => secondaryNameFragments.Any(fragment => f.Name.Contains(fragment))) ?? forestries.Find(f => f.Name.Split(" ").Length == 1);

                        break;
                    }
                }

                if (foundForestry != null)
                {
                    foundForestry.FullName = forestryName;
                    _forestriesRepository.UpdateFullName(foundForestry.Id, forestryName);
                }
            }

            return foundForestry;
        }

        private void UpdatePermit(Permit existingPermit, Permit newPermit, Enterprise newEnterprise, Forestry newForestry)
        {
            var changes = new List<string>();
            if(existingPermit.Region != newPermit.Region)
            {
                changes.Add($"Regionas: {existingPermit.Region} -> {newPermit.Region}");
                existingPermit.Region = newPermit.Region;
            }

            if (existingPermit.District != newPermit.District)
            {
                changes.Add($"Rajonas: {existingPermit.District} -> {newPermit.District}");
                existingPermit.District = newPermit.District;
            }

            if (existingPermit.OwnershipForm != newPermit.OwnershipForm)
            {
                changes.Add($"Nuosavybės forma: {existingPermit.OwnershipForm} -> {newPermit.OwnershipForm}");
                existingPermit.OwnershipForm = newPermit.OwnershipForm;
            }

            if (existingPermit.CadastralEnterpriseId != newPermit.CadastralEnterpriseId)
            {
                var existingEnterpriseFullName = _enterprisesRepository.GetFullName(existingPermit.CadastralForestryId);
                changes.Add($"Ūrėdija: {existingEnterpriseFullName} -> {newEnterprise.FullName}");
                existingPermit.CadastralEnterpriseId = newPermit.CadastralEnterpriseId;
            }

            if (existingPermit.CadastralForestryId != newPermit.CadastralForestryId)
            {
                var existingForestryFullName = _forestriesRepository.GetFullName(existingPermit.CadastralForestryId);
                changes.Add($"Girininkija: {existingForestryFullName} -> {newForestry.FullName}");
                existingPermit.CadastralForestryId = newPermit.CadastralForestryId;
            }

            if (existingPermit.CadastralLocation != newPermit.CadastralLocation)
            {
                changes.Add($"Kadastro vietovė: {existingPermit.CadastralLocation} -> {newPermit.CadastralLocation}");
                existingPermit.CadastralLocation = newPermit.CadastralLocation;
            }

            if (existingPermit.CadastralBlock != newPermit.CadastralBlock)
            {
                changes.Add($"Kadastro blokas: {existingPermit.CadastralBlock} -> {newPermit.CadastralBlock}");
                existingPermit.CadastralBlock = newPermit.CadastralBlock;
            }

            if (existingPermit.CadastralNumber != newPermit.CadastralNumber)
            {
                changes.Add($"Kadastro numeris: {existingPermit.CadastralNumber} -> {newPermit.CadastralNumber}");
                existingPermit.CadastralNumber = newPermit.CadastralNumber;
            }

            if (existingPermit.CuttingType != newPermit.CuttingType)
            {
                changes.Add($"Kirtimo tipas: {existingPermit.CuttingType} -> {newPermit.CuttingType}");
                existingPermit.CuttingType = newPermit.CuttingType;
            }

            if (existingPermit.ValidFrom != newPermit.ValidFrom)
            {
                changes.Add($"Galiojimo pradžia: {existingPermit.ValidFrom:yyyy-MM-dd} -> {newPermit.ValidFrom:yyyy-MM-dd}");
                existingPermit.ValidFrom = newPermit.ValidFrom;
            }

            if (existingPermit.ValidTo != newPermit.ValidTo)
            {
                changes.Add($"Galiojimo pabaiga: {existingPermit.ValidTo:yyyy-MM-dd} -> {newPermit.ValidTo:yyyy-MM-dd}");
                existingPermit.ValidTo = newPermit.ValidTo;
            }

            if (changes.Count > 0)
            {
                _permitsRepository.UpdatePermit(existingPermit);
                _permitsRepository.InsertPermitHistory(existingPermit.Id, changes);
            }
        }

        private void UpdatePermitSite(PermitSite existingPermitSite, PermitSite newPermitSite, int permitId)
        {
            var changes = new List<string>();
            if (existingPermitSite.Area != newPermitSite.Area)
            {
                changes.Add($"{string.Join(", ",existingPermitSite.SiteCodes)} sklypo(-ų) kirtimo plotas: {existingPermitSite.Area} -> {newPermitSite.Area}");
                existingPermitSite.Area = newPermitSite.Area;
            }

            if (!existingPermitSite.SiteCodes.All(newPermitSite.SiteCodes.Contains))
            {
                changes.Add($"Posklypiai: {string.Join(", ", existingPermitSite.SiteCodes)} -> {string.Join(", ", newPermitSite.SiteCodes)}");
                existingPermitSite.SiteCodes = newPermitSite.SiteCodes;
            }

            if (changes.Count > 0)
            {
                _permitsRepository.UpdatePermitSite(existingPermitSite);
                _permitsRepository.InsertPermitHistory(permitId, changes);
            }
        }

        private List<PermitSite> GetNewPermitSites(IGrouping<string, ScrapedPermit> permitGroupByBlock, Enterprise enterprise, Forestry forestry, PermitBlock permitBlock)
        {
            var defaultPermitBlockInfo = permitGroupByBlock.FirstOrDefault();
            var newPermitSites = new List<PermitSite>();
            foreach (var permitByBlock in permitGroupByBlock)
            {
                var sites = permitByBlock.Sites.Split(new[] { "-", ",", "/", @"\", ";", "/", "_" }, StringSplitOptions.RemoveEmptyEntries);
                var area = decimal.Parse(permitByBlock.Area);
                foreach (var site in sites)
                {
                    // Get site id by mu_kod, gir_kod, kv_nr and skl_nr | mu_kod: 52, gir_kod: 3, kv_nr: 10, skl_nr: 11 -> id: 718119
                    var cadastralSiteId = _sitesRepository.GetSiteId(enterprise.Code, forestry.Code, defaultPermitBlockInfo.Block, site);
                    if (cadastralSiteId.HasValue)
                    {
                        newPermitSites.Add(new PermitSite
                        {
                            PermitBlockId = permitBlock.Id,
                            CadastralSiteId = cadastralSiteId,
                            SiteCodes = new List<string> { site },
                            Area = area,
                        });
                        Console.WriteLine($"Site found");
                    }
                    else
                    {
                        var parentSite = Regex.Replace(site, "[^0-9.]", "");
                        if (site != parentSite)
                        {
                            var parentCadastralSiteId = _sitesRepository.GetSiteId(enterprise.Code, forestry.Code, defaultPermitBlockInfo.Block, parentSite);
                            if (parentCadastralSiteId.HasValue)
                            {
                                var existingPermitSite = newPermitSites.Find(p => p.CadastralSiteId == parentCadastralSiteId && p.Area == area);
                                if (existingPermitSite != null)
                                    existingPermitSite.SiteCodes.Add(site);
                                else
                                    newPermitSites.Add(new PermitSite
                                    {
                                        PermitBlockId = permitBlock.Id,
                                        CadastralSiteId = parentCadastralSiteId,
                                        SiteCodes = new List<string> { site },
                                        Area = area,
                                    });

                                Console.WriteLine($"Parent site found");
                            }
                            else
                            {
                                newPermitSites.Add(new PermitSite
                                {
                                    PermitBlockId = permitBlock.Id,
                                    CadastralSiteId = null,
                                    SiteCodes = new List<string> { site },
                                    Area = area,
                                });
                                Console.WriteLine($"Parent site not found: {parentSite}");
                            }
                        }
                        else
                        {
                            newPermitSites.Add(new PermitSite
                            {
                                PermitBlockId = permitBlock.Id,
                                CadastralSiteId = null,
                                SiteCodes = new List<string> { site },
                                Area = area,
                            });
                            Console.WriteLine($"Site not found: {site}");
                        }
                    }
                }
            }

            return newPermitSites;
        }
    }
}
