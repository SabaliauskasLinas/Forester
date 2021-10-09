namespace PermitsScraper.Services
{
    internal interface IScrapingService
    {
        void Run();
        void ScrapeOld();
    }
}