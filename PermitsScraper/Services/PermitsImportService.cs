using Entities.Cadastre;
using Entities.Import;
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

            var permitGroupsByNumber = args.ScrapedPermits.GroupBy(p => p.Number);
            foreach (var permitGroupByNumber in permitGroupsByNumber)
            {
                var generalPermitInfo = permitGroupByNumber.FirstOrDefault();
                var permit = new Permit
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

                permit.Id = _permitsRepository.InsertPermit(permit);
                var permitGroupsByBlock = permitGroupByNumber.GroupBy(pg => pg.Block);
                foreach(var permitGroupByBlock in permitGroupsByBlock)
                {
                    var defaultPermitBlockInfo = permitGroupByBlock.FirstOrDefault();
                    var cadastralBlockId = _blocksRepository.GetBlockId(enterprise.Code, forestry.Code, defaultPermitBlockInfo.Block);
                    if (!cadastralBlockId.HasValue)
                    {
                        Console.WriteLine($"Block not found");
                        continue;
                    }
                    var permitBlock = new PermitBlock
                    {
                        PermitId = permit.Id,
                        CadastralBlockId = cadastralBlockId.Value,
                    };

                    permitBlock.Id = _permitsRepository.InsertPermitBlock(permitBlock);
                    var permitSites = new List<PermitSite>();
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
                                permitSites.Add(new PermitSite
                                {
                                    PermitBlockId = permitBlock.Id,
                                    CadastralSideId = cadastralSiteId,
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
                                        var existingPermitSite = permitSites.Find(p => p.CadastralSideId == parentCadastralSiteId);
                                        if (existingPermitSite != null)
                                            existingPermitSite.SiteCodes.Add(parentSite);
                                        else
                                            permitSites.Add(new PermitSite
                                            {
                                                PermitBlockId = permitBlock.Id,
                                                CadastralSideId = parentCadastralSiteId,
                                                SiteCodes = new List<string> { site },
                                                Area = area,
                                            });

                                        Console.WriteLine($"Parent site found");
                                    }
                                    else
                                    {
                                        permitSites.Add(new PermitSite
                                        {
                                            PermitBlockId = permitBlock.Id,
                                            CadastralSideId = null,
                                            SiteCodes = new List<string> { site },
                                            Area = area,
                                        });
                                        Console.WriteLine($"Parent site not found: {parentSite} | Enterprise: {args.Enterprise}, Forestry: {args.Forestry}, Block: {defaultPermitBlockInfo.Block}");
                                    }
                                }
                                else
                                {
                                    permitSites.Add(new PermitSite
                                    {
                                        PermitBlockId = permitBlock.Id,
                                        CadastralSideId = null,
                                        SiteCodes = new List<string> { site },
                                        Area = area,
                                    });
                                    Console.WriteLine($"Site not found: {site} | Enterprise: {args.Enterprise}, Forestry: {args.Forestry}, Block: {defaultPermitBlockInfo.Block}");
                                }
                            }
                        }
                    }

                    foreach (var permitSite in permitSites) // TODO: Multiple insert
                        _permitsRepository.InsertPermitSite(permitSite);

                    var blockHasUnmappedSite = permitSites.Any(p => !p.CadastralSideId.HasValue);
                    if (blockHasUnmappedSite)
                        _permitsRepository.UpdateBlockHasUnmappedSite(permitBlock.Id, blockHasUnmappedSite);
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
            }

            //Console.WriteLine($"Enterprise: {enterpriseName}, Found mu_kod: {foundEnterprise.Code}");
            return foundEnterprise;
        }

        private Forestry GetForestry(int enterpriseCode, string forestryName)
        {
            Forestry foundForestry = null;
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

            //if (foundForestry == null)
            //{
            //    Console.WriteLine($"-------------------------------------------------------------");
            //    Console.WriteLine($"ERROR NOT FOUND - mu_kod: {enterpriseCode}, forestryName: {forestryName}");
            //    Console.WriteLine($"-------------------------------------------------------------");
            //}
            //else
            //    Console.WriteLine($"mu_kod: {enterpriseCode}, Found gir_kod: {foundForestry.Code}");

            return foundForestry;
        }
    }
}
