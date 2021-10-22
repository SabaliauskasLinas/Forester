using Entities.Permits;

namespace PermitsScraper.Services
{
    public interface IPermitsImportService
    {
        void Import(PermitsImportArgs args);
    }
}