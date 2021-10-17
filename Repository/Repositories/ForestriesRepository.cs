using Entities.Cadastre;
using Entities.Repository;
using System.Collections.Generic;

namespace Repository.Repositories
{
    public class ForestriesRepository : IForestriesRepository
    {
        private readonly IDbRepository _repository;

        public ForestriesRepository(IDbRepository repository)
        {
            _repository = repository;
        }

        public List<Forestry> GetForestries(int enterpriseCode, string nameFragment)
        {
            var sql = new RawSqlCommand(@"
                SELECT id, gir_kod AS Code, pavadinima AS Name
                FROM forestries
                WHERE mu_kod = @enterpriseCode AND pavadinima LIKE CONCAT('%', @nameFragment, '%');
            ");

            sql.AddParameter("enterpriseCode", enterpriseCode.ToString());
            sql.AddParameter("nameFragment", nameFragment);

            return _repository.RawSqlFetchList<Forestry>(sql);
        }
    }
}
