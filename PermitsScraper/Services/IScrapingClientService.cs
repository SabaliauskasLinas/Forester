using PermitsScraper.Entities;
using RestSharp;

namespace PermitsScraper.Services
{
    public interface IScrapingClientService
    {
        IRestResponse GetPageResponse();
        IRestResponse GetPageResponse(GetPageArgs args);
    }
}