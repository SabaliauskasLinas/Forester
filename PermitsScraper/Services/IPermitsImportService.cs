using Entities.Permits;

namespace PermitsScraper.Services
{
    public interface IPermitsImportService
    {
        PermitsImportResult Import(PermitsImportArgs args);
    }
}