using Entities.Cadastre;
using System.Collections.Generic;

namespace Repository.Repositories
{
    public interface IEnterprisesRepository
    {
        string GetFullName(int cadastralEnterpriseId);
        Enterprise GetEnterpriseByFullName(string fullName);
        List<Enterprise> GetEnterprisesByNameFragment(string nameFragment);
        void UpdateFullName(int enterpriseId, string fullName);
    }
}