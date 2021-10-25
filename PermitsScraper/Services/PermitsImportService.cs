using Common;
using Common.Log;
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
        private readonly ILog _log;
        private readonly IEnterprisesRepository _enterprisesRepository;
        private readonly IForestriesRepository _forestriesRepository;
        private readonly IBlocksRepository _blocksRepository;
        private readonly ISitesRepository _sitesRepository;
        private readonly IPermitsRepository _permitsRepository;
        private readonly Dictionary<string, Enterprise> _enterprisesCache;

        public PermitsImportService(ILogProvider logProvider, IEnterprisesRepository enterprisesRepository, IForestriesRepository forestriesRepository, IBlocksRepository blocksRepository, ISitesRepository sitesRepository, IPermitsRepository permitsRepository)
        {
            _log = logProvider.Get<PermitsImportService>();
            _enterprisesRepository = enterprisesRepository;
            _forestriesRepository = forestriesRepository;
            _blocksRepository = blocksRepository;
            _sitesRepository = sitesRepository;
            _permitsRepository = permitsRepository;
            _enterprisesCache = new Dictionary<string, Enterprise>();
        }

        public PermitsImportResult Import(PermitsImportArgs args)
        {
            _log.Info($"Import started for Enterprise - [{args.Enterprise}], Forestry - [{args.Forestry}]");
            var result = new PermitsImportResult();
            // Get enterprise id and mu_kod by enteprise name | name: 'Kazlų Rūdos miškų ūredija' -> id: 7, mu_kod: 52
            var enterprise = GetEnterprise(args.Enterprise);
            if (enterprise == null)
            {
                _log.Error($"Enterprise not found [enterprise = {args.Enterprise}]");
                result.TotalPermitsFailed = args.ScrapedPermits.GroupBy(p => p.Number).Count();
                return result;
            }
            // Get forestry id and gir_kod by mu_kod and forestry name | mu_kod: 52, name: 'Agurkiškės girininkija' ->  id: 83, gir_kod: 3
            var forestry = GetForestry(enterprise.Code, args.Forestry);
            if (forestry == null)
            {
                _log.Error($"Forestry not found [forestry = {args.Forestry}, enterprise = {args.Enterprise}]");
                result.TotalPermitsFailed = args.ScrapedPermits.GroupBy(p => p.Number).Count();
                return result;
            }

            // Group all scraped permits by permit number and iterate through groups
            foreach (var permitGroupByNumber in args.ScrapedPermits.GroupBy(p => p.Number))
            {
                var permitPartiallyFailed = false;
                var permitUpdated = false;
                var generalPermitInfo = permitGroupByNumber.FirstOrDefault();
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

                // If permit exists - update, otherwise - insert
                var existingPermit = _permitsRepository.GetFullPermit(generalPermitInfo.Number);
                if (existingPermit != null)
                {
                    var changes = GetPermitChanges(existingPermit, newPermit, enterprise, forestry);
                    if (changes.Count > 0)
                    {
                        _permitsRepository.UpdatePermit(existingPermit);
                        _permitsRepository.InsertPermitHistory(existingPermit.Id, changes);
                        result.TotalPermitsUpdated++;
                        permitUpdated = true;
                    }
                    newPermit.Id = existingPermit.Id;
                }
                else
                {
                    newPermit.Id = _permitsRepository.InsertPermit(newPermit);
                    result.TotalPermitsInserted++;
                }

                _log.Debug($"New permit: [{newPermit.JsonToString()}]");

                // Each one of the existing permit blocks needs to be found in a new data - otherwise, all remaining will be deleted
                var unhandledExistingPermitBlocks = new List<PermitBlock>();
                if (existingPermit?.PermitBlocks?.Count > 0)
                    unhandledExistingPermitBlocks.AddRange(existingPermit.PermitBlocks);

                var newPermitBlocks = new List<PermitBlock>();
                // Grouped scraped permits by a permit number are grouped again by block and iterated through each group
                foreach (var permitGroupByBlock in permitGroupByNumber.GroupBy(pg => pg.Block))
                {
                    var generalPermitBlockInfo = permitGroupByBlock.FirstOrDefault();
                    var cadastralBlockId = _blocksRepository.GetBlockId(enterprise.Code, forestry.Code, generalPermitBlockInfo.Block);
                    if (!cadastralBlockId.HasValue)
                    {
                        _log.Error($"Block not found [block = {generalPermitBlockInfo.Block}, forestry = {args.Forestry}, enterprise = {args.Enterprise}]");
                        if (!permitPartiallyFailed)
                        {
                            result.TotalPermitsPartiallyFailed++;
                            permitPartiallyFailed = true;
                        }
                        continue;
                    }
                    var newPermitBlock = new PermitBlock
                    {
                        PermitId = newPermit.Id,
                        CadastralBlockId = cadastralBlockId.Value,
                    };

                    var existingPermitBlock = existingPermit?.PermitBlocks?.Find(pb => pb.CadastralBlockId == cadastralBlockId.Value);
                    if (existingPermitBlock != null)
                    {
                        unhandledExistingPermitBlocks.Remove(existingPermitBlock);
                        newPermitBlock.Id = existingPermitBlock.Id;
                    }
                    else
                    {
                        newPermitBlock.Id = _permitsRepository.InsertPermitBlock(newPermitBlock);
                        if (existingPermit != null)
                        {
                            _permitsRepository.InsertPermitHistory(newPermit.Id, $"Naujas kvartalas: {generalPermitBlockInfo.Block}");
                            AddTotalUpdated(result, ref permitUpdated);
                        }
                    }

                    _log.Debug($"New permit block: [{newPermitBlock.JsonToString()}]");

                    // Get all sites in the given block
                    newPermitBlock.PermitSites = GetNewPermitSites(permitGroupByBlock, enterprise, forestry, newPermitBlock);
                    newPermitBlocks.Add(newPermitBlock);

                    // Each one of the existing permit sites needs to be found in a new data - otherwise, all remaining will be deleted
                    var unhandledExistingPermitSites = new List<PermitSite>();
                    if (existingPermitBlock?.PermitSites?.Count > 0)
                        unhandledExistingPermitSites.AddRange(existingPermitBlock.PermitSites);

                    foreach (var newPermitSite in newPermitBlock.PermitSites)
                    {
                        if (unhandledExistingPermitSites.Count > 0)
                        {
                            // Try to find existing permit site by cadastral site ID
                            var existingPermitSite = unhandledExistingPermitSites.Find(ps => ps.CadastralSiteId != null && ps.CadastralSiteId == newPermitSite.CadastralSiteId);
                            if (existingPermitSite != null)
                            {
                                unhandledExistingPermitSites.Remove(existingPermitSite);
                                var changes = GetPermitSiteChanges(existingPermitSite, newPermitSite);
                                if (changes.Count > 0)
                                {
                                    _permitsRepository.UpdatePermitSite(existingPermitSite);
                                    _permitsRepository.InsertPermitHistory(existingPermit.Id, changes);
                                    AddTotalUpdated(result, ref permitUpdated);
                                }
                            }
                            else
                            {
                                // Try to find existing unmapped permit site by site code (for area update)
                                var existingUnmappedPermitSite = unhandledExistingPermitSites.Find(ps => ps.CadastralSiteId == null && newPermitSite.CadastralSiteId == null && ps.SiteCodes[0] == newPermitSite.SiteCodes[0]);
                                if (existingUnmappedPermitSite != null)
                                {
                                    unhandledExistingPermitSites.Remove(existingPermitSite);
                                    var changes = GetPermitSiteChanges(existingUnmappedPermitSite, newPermitSite);
                                    if (changes.Count > 0)
                                    {
                                        _permitsRepository.UpdatePermitSite(existingUnmappedPermitSite);
                                        _permitsRepository.InsertPermitHistory(existingPermit.Id, changes);
                                        AddTotalUpdated(result, ref permitUpdated);
                                    }
                                }
                                else
                                {
                                    _permitsRepository.InsertPermitSite(newPermitSite);
                                    _permitsRepository.InsertPermitHistory(existingPermit.Id, newPermitSite.SiteCodes.Count > 1 ? $"Nauji sklypai: {string.Join(", ", newPermitSite.SiteCodes)}" : $"Naujas sklypas: {newPermitSite.SiteCodes[0]}");
                                    AddTotalUpdated(result, ref permitUpdated);
                                }
                            }
                        }
                        else
                        {
                            _permitsRepository.InsertPermitSite(newPermitSite);
                            if (existingPermit != null)
                            {
                                _permitsRepository.InsertPermitHistory(newPermit.Id, newPermitSite.SiteCodes.Count > 1 ? $"Nauji sklypai: {string.Join(", ", newPermitSite.SiteCodes)}" : $"Naujas sklypas: {newPermitSite.SiteCodes[0]}");
                                AddTotalUpdated(result, ref permitUpdated);
                            }
                        }
                    }

                    // Delete removed permit sites
                    if (unhandledExistingPermitSites.Count > 0)
                    {
                        _permitsRepository.DeletePermitSites(unhandledExistingPermitSites.Select(ps => ps.Id).ToList());
                        foreach (var unhandledExistingPermitSite in unhandledExistingPermitSites)
                            _permitsRepository.InsertPermitHistory(existingPermit.Id, unhandledExistingPermitSite.SiteCodes.Count > 1 ? $"Ištrinti sklypai: {string.Join(", ", unhandledExistingPermitSite.SiteCodes)}" : $"Ištrintas sklypas: {unhandledExistingPermitSite.SiteCodes[0]}");

                        AddTotalUpdated(result, ref permitUpdated);
                    }


                    // Check if block has unmapped sites
                    var blockHasUnmappedSite = newPermitBlock.PermitSites.Any(p => !p.CadastralSiteId.HasValue);
                    if (existingPermitBlock != null && existingPermitBlock.HasUnmappedSites != blockHasUnmappedSite)
                    {
                        existingPermitBlock.HasUnmappedSites = blockHasUnmappedSite;
                        _permitsRepository.UpdatePermitBlock(existingPermitBlock);
                        AddTotalUpdated(result, ref permitUpdated);
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

                    AddTotalUpdated(result, ref permitUpdated);
                }
            }

            return result;
        }

        private void AddTotalUpdated(PermitsImportResult result, ref bool permitUpdated)
        {
            if (!permitUpdated)
            {
                result.TotalPermitsUpdated++;
                permitUpdated = true;
            }
        }

        private Enterprise GetEnterprise(string enterpriseName)
        {
            Enterprise foundEnterprise = null;
            if (_enterprisesCache.TryGetValue(enterpriseName, out Enterprise cachedEnterprise))
                foundEnterprise = cachedEnterprise;

            if (foundEnterprise == null)
            {
                // Check if it is possible to find an enterprise by filter value (full name)
                var enterprise = _enterprisesRepository.GetEnterpriseByFullName(enterpriseName);
                if (enterprise != null)
                {
                    foundEnterprise = enterprise;
                    _enterprisesCache.Add(enterpriseName, foundEnterprise);
                    _log.Debug($"Enterprise [{foundEnterprise.FullName}] found by the full name - {enterpriseName}");
                }
                else
                {
                    // Split filter value (full name) into word fragments and iterate through each one of them until enterprise is found
                    var enterpriseNameFragments = enterpriseName.Split(" ").ToList();
                    foreach (var primaryNameFragment in enterpriseNameFragments)
                    {
                        var enterprises = _enterprisesRepository.GetEnterprisesByNameFragment(primaryNameFragment);
                        // If only one enterprise is found by the name fragment - it's a match
                        if (enterprises.Count == 1)
                        {
                            foundEnterprise = enterprises[0];
                            _enterprisesCache.Add(enterpriseName, foundEnterprise);
                            _log.Debug($"Enterprise [{foundEnterprise.FullName ?? foundEnterprise.Name}] found by the primary name fragment - {primaryNameFragment}");
                            break;
                        }
                        /* If there are several enterprises found by the gyven name fragment - exclude the primary name fragment and search by the other ones
                        * E.g. 
                        * enterpriseName = "Naujosios Akmenės ūrėdija", primaryNameFragment = "Naujosios"
                        * Results from database: "Naujosios Akmenės", "Naujosios Vilnės"
                        * foundEnterprise = "Naujosios Akmenės" by secondaryNameFragment = "Akmenės"
                        */
                        else if (enterprises.Count > 1)
                        {
                            var secondaryNameFragments = new List<string>(enterpriseNameFragments);
                            secondaryNameFragments.Remove(primaryNameFragment);
                            foundEnterprise = enterprises.Find(e => secondaryNameFragments.Any(fragment => e.Name.Contains(fragment))) ?? enterprises.Find(e => e.Name.Split(" ").Length == 1);
                            if (foundEnterprise != null)
                            {
                                _enterprisesCache.Add(enterpriseName, foundEnterprise);
                                _log.Debug($"Enterprise [{foundEnterprise.FullName ?? foundEnterprise.Name}] found by one of the secondary name fragments - {string.Join(",", secondaryNameFragments)}");
                                break;
                            }
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
            // Check if it is possible to find a forestry by filter value (full name) in a given enterprise
            var forestry = _forestriesRepository.GetForestryByFullName(enterpriseCode, forestryName);
            if (forestry != null)
            {
                foundForestry = forestry;
                _log.Debug($"Forestry [{foundForestry.FullName}] found by full name - {forestryName}");
            }
            else
            {
                // Split filter value (full name) into word fragments and iterate through each one of them until forestry is found
                var forestryNameFragments = forestryName.Split(" ").ToList();
                foreach (var primaryNameFragment in forestryNameFragments)
                {
                    var forestries = _forestriesRepository.GetForestries(enterpriseCode, primaryNameFragment);
                    // If only one forestry is found by the name fragment in a given enterprise - it's a match
                    if (forestries.Count == 1)
                    {
                        foundForestry = forestries[0];
                        _log.Debug($"Forestry [{foundForestry.FullName ?? foundForestry.Name}] found by the primary name fragment - {primaryNameFragment}");
                        break;
                    }
                    /* If there are several forestries found by the gyven name fragment - exclude the primary name fragment and search by the other ones
                    * E.g. 
                    * forestryName = "Naujosios Akmenės girininkija", primaryNameFragment = "Naujosios"
                    * Results from database: "Naujosios Akmenės", "Naujosios Vilnės"
                    * foundForestry = "Naujosios Akmenės" by secondaryNameFragment = "Akmenės"
                    */
                    else if (forestries.Count > 1)
                    {
                        var secondaryNameFragments = new List<string>(forestryNameFragments);
                        secondaryNameFragments.Remove(primaryNameFragment);
                        foundForestry = forestries.Find(f => secondaryNameFragments.Any(fragment => f.Name.Contains(fragment))) ?? forestries.Find(f => f.Name.Split(" ").Length == 1);
                        _log.Debug($"Forestry [{foundForestry.FullName ?? foundForestry.Name}] found by one of the secondary name fragments - {string.Join(",", secondaryNameFragments)}");
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

        private List<string> GetPermitChanges(Permit existingPermit, Permit newPermit, Enterprise newEnterprise, Forestry newForestry)
        {
            var changes = new List<string>();
            if (existingPermit.Region != newPermit.Region)
            {
                changes.Add($"Regionas: {existingPermit.Region} -> {newPermit.Region}");
                existingPermit.Region = newPermit.Region;
                _log.Debug($"Permit [id = {existingPermit.Id}] region changed");
            }

            if (existingPermit.District != newPermit.District)
            {
                changes.Add($"Rajonas: {existingPermit.District} -> {newPermit.District}");
                existingPermit.District = newPermit.District;
                _log.Debug($"Permit [id = {existingPermit.Id}] district changed");
            }

            if (existingPermit.OwnershipForm != newPermit.OwnershipForm)
            {
                changes.Add($"Nuosavybės forma: {existingPermit.OwnershipForm} -> {newPermit.OwnershipForm}");
                existingPermit.OwnershipForm = newPermit.OwnershipForm;
                _log.Debug($"Permit [id = {existingPermit.Id}] ownership form changed");
            }

            if (existingPermit.CadastralEnterpriseId != newPermit.CadastralEnterpriseId)
            {
                var existingEnterpriseFullName = _enterprisesRepository.GetFullName(existingPermit.CadastralForestryId);
                changes.Add($"Ūrėdija: {existingEnterpriseFullName} -> {newEnterprise.FullName}");
                existingPermit.CadastralEnterpriseId = newPermit.CadastralEnterpriseId;
                _log.Debug($"Permit [id = {existingPermit.Id}] enterprise changed");
            }

            if (existingPermit.CadastralForestryId != newPermit.CadastralForestryId)
            {
                var existingForestryFullName = _forestriesRepository.GetFullName(existingPermit.CadastralForestryId);
                changes.Add($"Girininkija: {existingForestryFullName} -> {newForestry.FullName}");
                existingPermit.CadastralForestryId = newPermit.CadastralForestryId;
                _log.Debug($"Permit [id = {existingPermit.Id}] forestry changed");
            }

            if (existingPermit.CadastralLocation != newPermit.CadastralLocation)
            {
                changes.Add($"Kadastro vietovė: {existingPermit.CadastralLocation} -> {newPermit.CadastralLocation}");
                existingPermit.CadastralLocation = newPermit.CadastralLocation;
                _log.Debug($"Permit [id = {existingPermit.Id}] cadastral location changed");
            }

            if (existingPermit.CadastralBlock != newPermit.CadastralBlock)
            {
                changes.Add($"Kadastro blokas: {existingPermit.CadastralBlock} -> {newPermit.CadastralBlock}");
                existingPermit.CadastralBlock = newPermit.CadastralBlock;
                _log.Debug($"Permit [id = {existingPermit.Id}] cadastral block changed");
            }

            if (existingPermit.CadastralNumber != newPermit.CadastralNumber)
            {
                changes.Add($"Kadastro numeris: {existingPermit.CadastralNumber} -> {newPermit.CadastralNumber}");
                existingPermit.CadastralNumber = newPermit.CadastralNumber;
                _log.Debug($"Permit [id = {existingPermit.Id}] cadastral number changed");
            }

            if (existingPermit.CuttingType != newPermit.CuttingType)
            {
                changes.Add($"Kirtimo tipas: {existingPermit.CuttingType} -> {newPermit.CuttingType}");
                existingPermit.CuttingType = newPermit.CuttingType;
                _log.Debug($"Permit [id = {existingPermit.Id}] cutting type changed");
            }

            if (existingPermit.ValidFrom != newPermit.ValidFrom)
            {
                changes.Add($"Galiojimo pradžia: {existingPermit.ValidFrom:yyyy-MM-dd} -> {newPermit.ValidFrom:yyyy-MM-dd}");
                existingPermit.ValidFrom = newPermit.ValidFrom;
                _log.Debug($"Permit [id = {existingPermit.Id}] valid from changed");
            }

            if (existingPermit.ValidTo != newPermit.ValidTo)
            {
                changes.Add($"Galiojimo pabaiga: {existingPermit.ValidTo:yyyy-MM-dd} -> {newPermit.ValidTo:yyyy-MM-dd}");
                existingPermit.ValidTo = newPermit.ValidTo;
                _log.Debug($"Permit [id = {existingPermit.Id}] valid to changed");
            }

            return changes;
        }

        private List<string> GetPermitSiteChanges(PermitSite existingPermitSite, PermitSite newPermitSite)
        {
            var changes = new List<string>();
            if (existingPermitSite.Area != newPermitSite.Area)
            {
                changes.Add($"{string.Join(", ", existingPermitSite.SiteCodes)} sklypo(-ų) kirtimo plotas: {existingPermitSite.Area} -> {newPermitSite.Area}");
                existingPermitSite.Area = newPermitSite.Area;
                _log.Debug($"Permit site [id = {existingPermitSite.Id}] area changed");
            }

            if (!existingPermitSite.SiteCodes.All(newPermitSite.SiteCodes.Contains))
            {
                changes.Add($"Posklypiai: {string.Join(", ", existingPermitSite.SiteCodes)} -> {string.Join(", ", newPermitSite.SiteCodes)}");
                existingPermitSite.SiteCodes = newPermitSite.SiteCodes;
                _log.Debug($"Permit site [id = {existingPermitSite.Id}] sites codes changed");
            }

            return changes;
        }

        private List<PermitSite> GetNewPermitSites(IGrouping<string, ScrapedPermit> permitGroupByBlock, Enterprise enterprise, Forestry forestry, PermitBlock newPermitBlock)
        {
            var generalPermitBlockInfo = permitGroupByBlock.FirstOrDefault();
            var newPermitSites = new List<PermitSite>();
            // Iterate through all permit rows which are all in the same block
            foreach (var permitByBlock in permitGroupByBlock)
            {
                var sites = permitByBlock.Sites.Split(new[] { "-", ",", "/", @"\", ";", "/", "_" }, StringSplitOptions.RemoveEmptyEntries);
                var area = decimal.Parse(permitByBlock.Area);
                foreach (var site in sites)
                {
                    // Get site id by mu_kod, gir_kod, kv_nr and skl_nr | mu_kod: 52, gir_kod: 3, kv_nr: 10, skl_nr: 11 -> id: 718119
                    var cadastralSiteId = _sitesRepository.GetSiteId(enterprise.Code, forestry.Code, generalPermitBlockInfo.Block, site);
                    if (cadastralSiteId.HasValue)
                    {
                        newPermitSites.Add(new PermitSite
                        {
                            PermitBlockId = newPermitBlock.Id,
                            CadastralSiteId = cadastralSiteId,
                            SiteCodes = new List<string> { site },
                            Area = area,
                        });
                    }
                    else
                    {
                        // If cadastral site ID is not found, search for a parent site (e.g. 7ab -> 7, 8c -> 8)
                        var parentSite = Regex.Replace(site, "[^0-9.]", "");
                        if (site != parentSite)
                        {
                            var parentCadastralSiteId = _sitesRepository.GetSiteId(enterprise.Code, forestry.Code, generalPermitBlockInfo.Block, parentSite);
                            if (parentCadastralSiteId.HasValue)
                            {
                                var existingPermitSite = newPermitSites.Find(p => p.CadastralSiteId == parentCadastralSiteId && p.Area == area);
                                if (existingPermitSite != null)
                                    existingPermitSite.SiteCodes.Add(site);
                                else
                                    newPermitSites.Add(new PermitSite
                                    {
                                        PermitBlockId = newPermitBlock.Id,
                                        CadastralSiteId = parentCadastralSiteId,
                                        SiteCodes = new List<string> { site },
                                        Area = area,
                                    });
                            }
                            else
                            {
                                newPermitSites.Add(new PermitSite
                                {
                                    PermitBlockId = newPermitBlock.Id,
                                    CadastralSiteId = null,
                                    SiteCodes = new List<string> { site },
                                    Area = area,
                                });
                                _log.Warn($"Site not found by parent [site = {site}, parentSite  = {parentSite}, forestry = {forestry.FullName}, enterprise = {enterprise.FullName}]");
                            }
                        }
                        else
                        {
                            newPermitSites.Add(new PermitSite
                            {
                                PermitBlockId = newPermitBlock.Id,
                                CadastralSiteId = null,
                                SiteCodes = new List<string> { site },
                                Area = area,
                            });
                            _log.Warn($"Site not found [site = {site}, forestry = {forestry.FullName}, enterprise = {enterprise.FullName}]");
                        }
                    }
                }
            }

            return newPermitSites;
        }
    }
}
