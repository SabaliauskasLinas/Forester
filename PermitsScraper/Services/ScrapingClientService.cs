using Common;
using Entities.Scraping;
using Microsoft.Extensions.Configuration;
using RestSharp;

namespace PermitsScraper.Services
{
    public class ScrapingClientService : IScrapingClientService
    {
        private readonly RestClient _client;
        public ScrapingClientService(IConfigurationRoot config)
        {
            _client = new RestClient(config.GetSection("ForestPermitsUrl").Value);
        }

        public IRestResponse GetWebsiteResponse() => _client.Execute(new RestRequest(Method.GET));

        public IRestResponse GetWebsiteResponse(GetWebsiteArgs args)
        {
            var request = new RestRequest(Method.POST);
            request.AddHeader("Content-Type", "application/x-www-form-urlencoded");
            request.AddHeader("Cookie", args.Cookie);
            request.AddParameter("application/x-www-form-urlencoded", args.GetQueryString(), ParameterType.RequestBody);

            return _client.Execute(request);
        }
    }
}
