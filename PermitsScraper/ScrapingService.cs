using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using PermitsScraper.Entities;
using Repository.Repositories;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PermitsScraper
{
    public class ScrapingService
    {
        private readonly Regex _regex = new Regex(@"<tr[^>]*>\s*<td>([^<]+)<\/td><td>([^<]+)<\/td><td>([^<]+)<\/td><td>([^<]+)<\/td><td>([^<]+)<\/td><td>([^<]+)<\/td><td>([^<]+)<\/td><td>([^<]+)<\/td><td>([^<]+)<\/td><td>([^<]+)<\/td><td>([^<]+)<\/td><td>([^<]+)<\/td><td>([^<]+)<\/td><td>([^<]+)<\/td><td>([^<]+)<\/td>\s*<\/tr>");
        public void Scrape()
        {
            //using (ObjectContainer.BeginContext)
            //{
            //    var repo = ObjectContainer.GetInstance<ITestRepo>();

            //    repo.TestDatabase();
            //    repo.TestDatabase2();
            //}
            ChromeOptions chromeOptions = new ChromeOptions();
            //chromeOptions.AddArgument("headless");
            using (IWebDriver driver = new ChromeDriver(chromeOptions))
            {
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(10));
                driver.Navigate().GoToUrl("http://www.amvmt.lt/kirtleidimai/default.aspx");
                var enterprisesRadio = driver.FindElement(By.Id("RadioButton4"));
                enterprisesRadio.Click();
                var dropdown = driver.FindElement(By.Id("DropDownList3"));
                var options = dropdown.FindElements(By.TagName("option"));
                var optionValues = options.Select(o => o.GetAttribute("value")).ToList();
                foreach (var optionValue in optionValues)
                {
                    dropdown = driver.FindElement(By.Id("DropDownList3"));
                    dropdown.Click();
                    var option = driver.FindElement(By.XPath($"//option[@value='{optionValue}']"));
                    option.Click();
                    driver.FindElement(By.Id("Button1")).Click();

                    var paginationExists = driver.FindElements(By.XPath("//table[@id='GridView2']/tbody/tr[@align='right']")).Count == 1;
                    var permits = GetData(driver, paginationExists);
                    if (paginationExists)
                    {
                        var currentPage = 1;
                        while (true)
                        {
                            currentPage++;
                            var pagination = driver.FindElement(By.XPath("//table[@id='GridView2']/tbody/tr[@align='right']"));
                            if (pagination.FindElements(By.XPath($"//a[contains(@href, \"'Page${currentPage}'\")]")).Count == 0)
                                break;

                            pagination.FindElement(By.XPath($"//a[contains(@href, \"'Page${currentPage}'\")]")).Click();
                            permits.AddRange(GetData(driver, paginationExists));
                        }
                        GoToFirstPage(driver);
                    }
                    Console.WriteLine($"Enterprise: {optionValue}, Permits Count: {permits.Count}");
                }
            }
        }

        private void GoToFirstPage(IWebDriver driver)
        {
            IJavaScriptExecutor js = (IJavaScriptExecutor)driver;
            js.ExecuteScript("javascript: __doPostBack('GridView2', 'Page$1')");
        }

        private List<Permit> GetData(IWebDriver driver, bool paginationExists)
        {
            Stopwatch stopWatch = new Stopwatch();

            //stopWatch.Start();
            //var results = GetDataV2(driver);
            //stopWatch.Stop();

            //Console.WriteLine($"V2 Time elapsed: {stopWatch.Elapsed.TotalSeconds}");
            //stopWatch.Reset();

            //stopWatch.Start();
            //var results = GetDataV1(driver, paginationExists);
            //stopWatch.Stop();


            stopWatch.Start();
            var results = GetDataV3(driver);
            stopWatch.Stop();

            StreamWriter sw = new StreamWriter("D:\\Test.txt", true);
            foreach (var result in results)
                sw.WriteLine($"{result.Number}, {result.Region}, {result.District}, {result.OwnershipForm}, {result.Enterprise}, {result.Forestry}, {result.Square}, {result.Plots}, {result.Area}, {result.CadastralLocation}, {result.CadastralBlock}, {result.CadastralNumber}, {result.CuttingType}, {result.ValidFrom}, {result.ValidTo}");
            sw.Close();

            Console.WriteLine($"V3 Time elapsed: {stopWatch.Elapsed.TotalSeconds}");
            return results;
        }

        private List<Permit> GetDataV1(IWebDriver driver, bool paginationExists)
        {
            var results = new List<Permit>();
            var tableRows = driver.FindElements(By.XPath("//table[@id='GridView2']/tbody/tr")).ToList();
            if (tableRows.Count == 0)
                return results;

            tableRows.RemoveAt(0);
            if (paginationExists)
                tableRows.RemoveAt(tableRows.Count - 1);

            foreach (var row in tableRows)
            {
                Stopwatch stopWatch = new Stopwatch();
                stopWatch.Start();
                var dataList = row.FindElements(By.TagName("td"));
                results.Add(new Permit
                {
                    Number = dataList[0].Text,
                    Region = dataList[1].Text,
                    District = dataList[2].Text,
                    OwnershipForm = dataList[3].Text,
                    Enterprise = dataList[4].Text,
                    Forestry = dataList[5].Text,
                    Square = dataList[6].Text,
                    Plots = dataList[7].Text,
                    Area = dataList[8].Text,
                    CadastralLocation = dataList[9].Text,
                    CadastralBlock = dataList[10].Text,
                    CadastralNumber = dataList[11].Text,
                    CuttingType = dataList[12].Text,
                    ValidFrom = dataList[13].Text,
                    ValidTo = dataList[14].Text,
                });
                stopWatch.Stop();
                Console.WriteLine($"Inner elapsed time {stopWatch.Elapsed.TotalSeconds}");
            }

            return results;
        }

        private List<Permit> GetDataV2(IWebDriver driver)
        {
            var tableData = driver.FindElements(By.XPath("//table[@id='GridView2']/tbody/tr/td"));
            var totalRows = tableData.Count / 15;
            var results = new List<Permit>();
            for (var i = 0; i < totalRows; i++)
            {
                var indexStart = i * 15;
                results.Add(new Permit
                {
                    Number = tableData[indexStart].Text,
                    Region = tableData[indexStart + 1].Text,
                    District = tableData[indexStart + 2].Text,
                    OwnershipForm = tableData[indexStart + 3].Text,
                    Enterprise = tableData[indexStart + 4].Text,
                    Forestry = tableData[indexStart + 5].Text,
                    Square = tableData[indexStart + 6].Text,
                    Plots = tableData[indexStart + 7].Text,
                    Area = tableData[indexStart + 8].Text,
                    CadastralLocation = tableData[indexStart + 9].Text,
                    CadastralBlock = tableData[indexStart + 10].Text,
                    CadastralNumber = tableData[indexStart + 11].Text,
                    CuttingType = tableData[indexStart + 12].Text,
                    ValidFrom = tableData[indexStart + 13].Text,
                    ValidTo = tableData[indexStart + 14].Text,
                });
            }
            return results;
        }

        private List<Permit> GetDataV3(IWebDriver driver)
        {
            var html = driver.PageSource;
            var matches = _regex.Matches(html);

            var results = new List<Permit>();
            foreach(Match match in matches)
            {
                var values = match.Groups.Values.ToArray();
                //match.Groups.
                //var permit = match.Groups.Select(v => new Permit
                //{
                //    Number = v,
                //    Region = tableData[indexStart + 1].Text,
                //    District = tableData[indexStart + 2].Text,
                //    OwnershipForm = tableData[indexStart + 3].Text,
                //    Enterprise = tableData[indexStart + 4].Text,
                //    Forestry = tableData[indexStart + 5].Text,
                //    Square = tableData[indexStart + 6].Text,
                //    Plots = tableData[indexStart + 7].Text,
                //    Area = tableData[indexStart + 8].Text,
                //    CadastralLocation = tableData[indexStart + 9].Text,
                //    CadastralBlock = tableData[indexStart + 10].Text,
                //    CadastralNumber = tableData[indexStart + 11].Text,
                //    CuttingType = tableData[indexStart + 12].Text,
                //    ValidFrom = tableData[indexStart + 13].Text,
                //    ValidTo = tableData[indexStart + 14].Text,
                //});
                results.Add(new Permit
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
            return results;
        }
    }
}
