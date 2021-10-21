using Entities.Import;

namespace Repository.Repositories
{
    public interface IPermitsRepository
    {
        int InsertPermit(Permit permit);
        int InsertPermitBlock(PermitBlock permitBlock);
        int InsertPermitSite(PermitSite permitSite);
        void UpdateBlockHasUnmappedSite(int permitBlockId, bool hasUnmappedSite);
    }
}