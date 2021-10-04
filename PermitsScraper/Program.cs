using HtmlAgilityPack;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using PermitsScraper.Entities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PermitsScraper
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ObjectContainer.Init();
            var service = new ScrapingService();
            service.Scrape();
        }
    }
}
