using Entities.Scraping;
using RestSharp;

namespace PermitsScraper.Services
{
    public interface IScrapingClientService
    {
        IRestResponse GetPageResponse();
        IRestResponse GetPageResponse(GetPageArgs args);
    }
}