using Entities.Permits;
using System.Collections.Generic;

namespace Repository.Repositories
{
    public interface IPermitsRepository
    {
        int InsertPermit(Permit permit);
        int InsertPermitBlock(PermitBlock permitBlock);
        int InsertPermitSite(PermitSite permitSite);
        void UpdateBlockHasUnmappedSite(int permitBlockId, bool hasUnmappedSite);
        Permit GetFullPermit(string permitNumber);
        void DeletePermitSites(List<int> ids);
    }
}