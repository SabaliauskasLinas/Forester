using Common;
using Microsoft.Extensions.Configuration;
using PermitsScraper.Entities;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace PermitsScraper.Services
{
    public class ScrapingClientService : IScrapingClientService
    {
        private readonly RestClient _client;
        public ScrapingClientService(IConfigurationRoot config)
        {
            _client = new RestClient(config.GetSection("ForestPermitsUrl").Value);
        }

        public IRestResponse GetPageResponse() => _client.Execute(new RestRequest(Method.GET));

        public IRestResponse GetPageResponse(GetPageArgs args)
        {
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddHeader("Cookie", args.Cookie);
            request.AddParameter("application/x-www-form-urlencoded", args.GetQueryString(), ParameterType.RequestBody);

            return _client.Execute(request);
        }
    }
}
