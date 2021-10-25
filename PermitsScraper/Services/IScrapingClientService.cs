using Entities.Scraping;
using RestSharp;

namespace PermitsScraper.Services
{
    public interface IScrapingClientService
    {
        IRestResponse GetWebsiteResponse();
        IRestResponse GetWebsiteResponse(GetWebsiteArgs args);
    }
}