using Entities.Import;

namespace PermitsScraper.Services
{
    public interface IPermitsImportService
    {
        void Import(PermitsImportArgs args);
    }
}