using Entities.Cadastre;
using System.Collections.Generic;

namespace Repository.Repositories
{
    public interface IForestriesRepository
    {
        List<Forestry> GetForestries(int enterpriseCode, string nameFragment);
    }
}