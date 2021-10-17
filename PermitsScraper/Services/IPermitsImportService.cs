using Entities.Scraping;

namespace PermitsScraper.Services
{
    public interface IPermitsImportService
    {
        void Import(PermitsImportArgs args);
    }
}