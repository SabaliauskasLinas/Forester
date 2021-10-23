using Entities.Cadastre;
using System.Collections.Generic;

namespace Repository.Repositories
{
    public interface IForestriesRepository
    {
        string GetFullName(int cadastralForestryId);
        Forestry GetForestryByFullName(int enterpriseCode, string fullName);
        List<Forestry> GetForestries(int enterpriseCode, string nameFragment);
        void UpdateFullName(int forestryId, string fullName);
    }
}