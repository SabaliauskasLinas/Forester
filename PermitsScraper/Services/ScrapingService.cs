﻿using HtmlAgilityPack;
using PermitsScraper.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PermitsScraper.Services
{
    public class ScrapingService : IScrapingService
    {
        private readonly Regex _tableRegex = new Regex(@"<tr[^>]*>\s*<td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td><td><font[^>]*>([^<]+)<\/font><\/td>\s*<\/tr>");
        private readonly IScrapingClientService _scrapingClientService;

        public ScrapingService(IScrapingClientService scrapingClientService)
        {
            _scrapingClientService = scrapingClientService;
        }

        public void Run()
        {
            var htmlDoc = new HtmlDocument();
            var pageResponse = _scrapingClientService.GetPageResponse();
            htmlDoc.LoadHtml(pageResponse.Content);
            var args = new GetPageArgs
            {
                Cookie = pageResponse.Headers.ToList().Find(x => x.Name == "Set-Cookie").Value.ToString().Split(";")[0],
                ViewState = htmlDoc.GetElementbyId("__VIEWSTATE").GetAttributeValue("value", string.Empty),
                Year = "RadioButton1",
                ReportType = "RadioButton5",
                FilterType = "RadioButton4",
                SortBy = "RB7_sort_Leid_serij_nr",
            };

            var enterprises = htmlDoc.DocumentNode.SelectNodes("//select[@id='DropDownList3']/option").Select(n => n.GetAttributeValue("value", string.Empty)).ToList();
            foreach (var enterprise in enterprises)
            {
                args.EventTarget = null;
                args.EventArgument = null;
                args.Enterprise = enterprise;

                pageResponse = _scrapingClientService.GetPageResponse(args);
                htmlDoc.LoadHtml(pageResponse.Content);
                args.ViewState = htmlDoc.GetElementbyId("__VIEWSTATE").GetAttributeValue("value", string.Empty);

                var forestries = htmlDoc.DocumentNode.SelectNodes("//select[@id='DropDownList4']/option").Select(n => n.GetAttributeValue("value", string.Empty)).ToList();
                foreach (var forestry in forestries)
                {
                    if (forestry == "Anciškių girininkija")
                        Console.WriteLine("A");
                    args.ForestryFilterState = "on";
                    args.Forestry = forestry;
                    args.ButtonName = "Filtruoti";
                    pageResponse = _scrapingClientService.GetPageResponse(args);
                    htmlDoc.LoadHtml(pageResponse.Content);
                    args.ViewState = htmlDoc.GetElementbyId("__VIEWSTATE").GetAttributeValue("value", string.Empty);

                    var permits = new List<Permit>();
                    permits.AddRange(GetPermits(pageResponse.Content));

                    var totalPages = int.Parse(htmlDoc.GetElementbyId("Label1").GetDirectInnerText().Split("  ")[1]);
                    if (totalPages > 1)
                    {
                        args.ButtonName = null;
                        args.EventTarget = "GridView2";
                        for (int page = 2; page <= totalPages; page++)
                        {
                            args.EventArgument = $"Page${page}";
                            pageResponse = _scrapingClientService.GetPageResponse(args);
                            permits.AddRange(GetPermits(pageResponse.Content));
                        }
                    }
                    Console.WriteLine($"{enterprise}: {forestry}, total permits - {permits.Count}");
                }
            }
        }

        private List<Permit> GetPermits(string page)
        {
            var permits = new List<Permit>();
            var matches = _tableRegex.Matches(page);
            foreach (Match match in matches)
            {
                var values = match.Groups.Values.ToArray();
                permits.Add(new Permit
                {
                    Number = values[1].Value,
                    Region = values[2].Value,
                    District = values[3].Value,
                    OwnershipForm = values[4].Value,
                    Enterprise = values[5].Value,
                    Forestry = values[6].Value,
                    Square = values[7].Value,
                    Plots = values[8].Value,
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