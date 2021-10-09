using Microsoft.Extensions.Configuration;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PermitsScraper.Services
{
    public class ScrapingClientService : IScrapingClientService
    {
        private readonly RestClient _client;
        public ScrapingClientService(IConfigurationRoot config)
        {
            _client = new RestClient(config.GetSection("ForestPermitsUrl").Value);
        }

        public void GetHtml()
        {
            var request = new RestRequest(Method.GET);
            IRestResponse response = _client.Execute(request);
            //Console.WriteLine(response.Content);

            var cookie = response.Headers.ToList()
                .Find(x => x.Name == "Set-Cookie")
                .Value.ToString().Split(";")[0];

            request = new RestRequest(Method.POST);
            request.AddHeader("Cookie", cookie);
            response = _client.Execute(request);
            Console.WriteLine(response.Content);
        }
    }
}
