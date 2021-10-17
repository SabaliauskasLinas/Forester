using Entities.Cadastre;
using System.Collections.Generic;

namespace Repository.Repositories
{
    public interface IEnterprisesRepository
    {
        List<Enterprise> GetEnterprisesByNameFragment(string nameFragment);
    }
}