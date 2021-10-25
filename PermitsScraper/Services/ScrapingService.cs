using Common.Log;
using Entities.Permits;
using Entities.Scraping;
using HtmlAgilityPack;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PermitsScraper.Services
{
    public class ScrapingService : IScrapingService
    {
        private readonly Regex _tableRegex = new Regex(@"<tr[^>]*>\s*<td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td>\s*<\/tr>");
        private readonly IScrapingClientService _scrapingClientService;
        private readonly IPermitsImportService _permitsImportService;
        private readonly ILog _log;

        public ScrapingService(ILogProvider logProvider, IScrapingClientService scrapingClientService, IPermitsImportService permitsImportService)
        {
            _log = logProvider.Get<ScrapingService>();
            _scrapingClientService = scrapingClientService;
            _permitsImportService = permitsImportService;
        }

        public void Run()
        {
            _log.Info("Scraping started");

            // Load a website to get a cookie and __VIEWSTATE which is required for ASP.NET requests
            var htmlDoc = new HtmlDocument();
            var websiteResponse = _scrapingClientService.GetWebsiteResponse();
            if (!websiteResponse.IsSuccessful || string.IsNullOrWhiteSpace(websiteResponse.Content))
                _log.Error("Remote server is down");

            htmlDoc.LoadHtml(websiteResponse.Content);
            var args = new GetWebsiteArgs
            {
                Cookie = websiteResponse.Headers.ToList().Find(x => x.Name == "Set-Cookie").Value.ToString().Split(";")[0],
                ViewState = htmlDoc.GetElementbyId("__VIEWSTATE").GetAttributeValue("value", string.Empty),
                Year = "RadioButton1",
                ReportType = "RadioButton5",
                FilterType = "RadioButton4",
                SortBy = "RB7_sort_Leid_serij_nr",
            };

            var totalInserted = 0;
            var totalUpdated = 0;
            var totalPartiallyFailed = 0;
            var totalFailed = 0;
            var totalPermitRows = 0;
            // Get all enterprises and iterate through each one of them
            var enterprises = htmlDoc.DocumentNode.SelectNodes("//select[@id='DropDownList3']/option").Select(n => n.GetAttributeValue("value", string.Empty)).ToList();
            foreach (var enterprise in enterprises)
            {
                args.EventTarget = null;
                args.EventArgument = null;
                args.Enterprise = enterprise;

                // Load a website with a selected enterprise
                websiteResponse = _scrapingClientService.GetWebsiteResponse(args);
                htmlDoc.LoadHtml(websiteResponse.Content);
                args.ViewState = htmlDoc.GetElementbyId("__VIEWSTATE").GetAttributeValue("value", string.Empty);

                var totalInsertedInEnterprise = 0;
                var totalUpdatedInEnterprise = 0;
                var totalPartiallyFailedInEnterprise = 0;
                var totalFailedInEnterprise = 0;
                var totalPermitRowsInEnterprise = 0;
                // Get all forestries in a particular enterprise and iterate through each one of them
                var forestries = htmlDoc.DocumentNode.SelectNodes("//select[@id='DropDownList4']/option").Select(n => n.GetAttributeValue("value", string.Empty)).ToList();
                foreach (var forestry in forestries)
                {
                    // Load a website by imitating a "Filter" button click
                    args.ForestryFilterState = "on";
                    args.Forestry = forestry;
                    args.ButtonName = "Filtruoti";
                    websiteResponse = _scrapingClientService.GetWebsiteResponse(args);
                    htmlDoc.LoadHtml(websiteResponse.Content);
                    args.ViewState = htmlDoc.GetElementbyId("__VIEWSTATE").GetAttributeValue("value", string.Empty);

                    // Parse all permits in the page
                    var permits = new List<ScrapedPermit>();
                    permits.AddRange(GetPermits(websiteResponse.Content));

                    // Check if there are more pages
                    var totalPages = int.Parse(htmlDoc.GetElementbyId("Label1").GetDirectInnerText().Split("  ")[1]);
                    if (totalPages > 1)
                    {
                        args.ButtonName = null;
                        args.EventTarget = "GridView2";
                        // Iterate through all pages
                        for (int page = 2; page <= totalPages; page++)
                        {
                            args.EventArgument = $"Page${page}";
                            websiteResponse = _scrapingClientService.GetWebsiteResponse(args);
                            // Parse all permits in the page
                            permits.AddRange(GetPermits(websiteResponse.Content));
                        }
                    }

                    _log.Info($"Scraping finished for Enterprise - [{enterprise}], Forestry - [{forestry}]. Total permit rows - {permits.Count}");
                    var result = _permitsImportService.Import(new PermitsImportArgs
                    {
                        Enterprise = enterprise,
                        Forestry = forestry,
                        ScrapedPermits = permits,
                    });

                    _log.Info($"Permits import finished for forestry {forestry} in {enterprise}: Total inserted = {result.TotalPermitsInserted}, total updated = {result.TotalPermitsUpdated}, total partially failed = {result.TotalPermitsPartiallyFailed}, total failed = {result.TotalPermitsFailed}, total permit rows = {permits.Count}");
                    totalPermitRowsInEnterprise += permits.Count;
                    totalInsertedInEnterprise += result.TotalPermitsInserted;
                    totalUpdatedInEnterprise += result.TotalPermitsUpdated;
                    totalPartiallyFailedInEnterprise += result.TotalPermitsPartiallyFailed;
                    totalFailedInEnterprise += result.TotalPermitsFailed;
                }

                _log.Info($"Permits import finished for enterprise {enterprise}: Total inserted = {totalInsertedInEnterprise}, total updated = {totalUpdatedInEnterprise}, total partially failed = {totalPartiallyFailedInEnterprise}, total failed = {totalFailedInEnterprise}, total permit rows = {totalPermitRowsInEnterprise}");
                totalInserted += totalInsertedInEnterprise;
                totalUpdated += totalUpdatedInEnterprise;
                totalPartiallyFailed += totalPartiallyFailedInEnterprise;
                totalFailed += totalFailedInEnterprise;
                totalPermitRows += totalPermitRowsInEnterprise;
            }

            _log.Info($"Permits import finished: Total inserted = {totalInserted}, total updated = {totalUpdated}, total partially failed = {totalPartiallyFailed}, total failed = {totalFailed}, total permit rows = {totalPermitRows}");
        }

        private List<ScrapedPermit> GetPermits(string page)
        {
            var permits = new List<ScrapedPermit>();
            var matches = _tableRegex.Matches(page);
            foreach (Match match in matches)
            {
                var values = match.Groups.Values.ToArray();
                permits.Add(new ScrapedPermit
                {
                    Number = values[1].Value,
                    Region = values[2].Value,
                    District = values[3].Value,
                    OwnershipForm = values[4].Value,
                    Enterprise = values[5].Value,
                    Forestry = values[6].Value,
                    Block = values[7].Value,
                    Sites = values[8].Value,
                    Area = values[9].Value,
                    CadastralLocation = values[10].Value,
                    CadastralBlock = values[11].Value,
                    CadastralNumber = values[12].Value,
                    CuttingType = values[13].Value,
                    ValidFrom = values[14].Value,
                    ValidTo = values[15].Value,
                });
            }

            return permits;
        }
    }
}
