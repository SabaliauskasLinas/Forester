using Entities.Cadastre;
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
        private readonly Dictionary<string, Enterprise> _enterprisesCache;

        public PermitsImportService(IEnterprisesRepository enterprisesRepository, IForestriesRepository forestriesRepository, IBlocksRepository blocksRepository, ISitesRepository sitesRepository)
        {
            _enterprisesRepository = enterprisesRepository;
            _forestriesRepository = forestriesRepository;
            _blocksRepository = blocksRepository;
            _sitesRepository = sitesRepository;
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
            // Iterate through permits and:
            foreach (var permit in args.Permits)
            {
                // Get block id by mu_kod, gir_kod and kv_nr | mu_kod: 52, gir_kod: 3, kv_nr: 10 -> id: 43222
                var blockId = _blocksRepository.GetBlockId(enterprise.Code, forestry.Code, permit.Block);
                if (!blockId.HasValue)
                {
                    Console.WriteLine($"Block not found: {permit.Block} | Enterprise: {args.Enterprise}, Forestry: {args.Forestry}");
                    continue;
                }

                var sites = permit.Sites.Split(new [] { "-", ",", "/", @"\", ";", "/", "_" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var site in sites)
                {
                    // Get site id by mu_kod, gir_kod, kv_nr and skl_nr | mu_kod: 52, gir_kod: 3, kv_nr: 10, skl_nr: 11 -> id: 718119
                    var siteId = _sitesRepository.GetSiteId(enterprise.Code, forestry.Code, permit.Block, site);
                    if (siteId.HasValue)
                        Console.WriteLine($"Site found  {enterprise.Code} {forestry.Code} {permit.Block} | {site}");
                    else
                    {
                        var parentSite = Regex.Replace(site, "[^0-9.]", "");
                        if (site != parentSite)
                        {
                            var parentSiteId = _sitesRepository.GetSiteId(enterprise.Code, forestry.Code, permit.Block, parentSite);
                            if (parentSiteId.HasValue)
                            {
                                Console.WriteLine($"Parent site found {enterprise.Code} {forestry.Code} {permit.Block} | {site} {parentSite}");
                            }
                            else
                                Console.WriteLine($"Parent site not found: {parentSite} | Enterprise: {args.Enterprise}, Forestry: {args.Forestry}, Block: {permit.Block}");
                        }
                        else
                            Console.WriteLine($"Site not found: {site} | Enterprise: {args.Enterprise}, Forestry: {args.Forestry}, Block: {permit.Block}");
                    }
                }
            }
            Console.WriteLine("------------------------------------");
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
