using Entities.Permits;
using System.Collections.Generic;

namespace Repository.Repositories
{
    public interface IPermitsRepository
    {
        int InsertPermit(Permit permit);
        int InsertPermitBlock(PermitBlock permitBlock);
        int InsertPermitSite(PermitSite permitSite);
        void UpdatePermit(Permit permit);
        void UpdatePermitBlock(PermitBlock permitBlock);
        void UpdatePermitSite(PermitSite permitSite);
        Permit GetFullPermit(string permitNumber);
        void DeletePermitSites(int permitBlockId);
        void DeletePermitSites(List<int> ids);
        void DeletePermitBlock(int id);
        void InsertPermitHistory(int permitId, string change);
        void InsertPermitHistory(int permitId, List<string> changes);
    }
}