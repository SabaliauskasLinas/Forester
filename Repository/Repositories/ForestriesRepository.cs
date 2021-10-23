using Common;
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

        public string GetFullName(int cadastralForestryId)
        {
            var sql = new RawSqlCommand(@"
                SELECT pavadinima_pilnas
                FROM enterprises
                WHERE id = @cadastralForestryId;
            ");

            sql.AddParameter("cadastralForestryId", cadastralForestryId);

            return _repository.RawSqlExecuteScalar(sql).ChangeType<string>();
        }

        public Forestry GetForestryByFullName(int enterpriseCode, string fullName)
        {
            var sql = new RawSqlCommand(@"
                SELECT id, gir_kod AS Code, pavadinima AS Name, pavadinima_pilnas AS FullName
                FROM forestries
                WHERE mu_kod = @enterpriseCode AND pavadinima_pilnas = @fullName;
            ");

            sql.AddParameter("enterpriseCode", enterpriseCode.ToString());
            sql.AddParameter("fullName", fullName);

            return _repository.RawSqlFetchSingle<Forestry>(sql);
        }

        public List<Forestry> GetForestries(int enterpriseCode, string nameFragment)
        {
            var sql = new RawSqlCommand(@"
                SELECT id, gir_kod AS Code, pavadinima AS Name, pavadinima_pilnas AS FullName
                FROM forestries
                WHERE mu_kod = @enterpriseCode AND pavadinima LIKE CONCAT('%', @nameFragment, '%');
            ");

            sql.AddParameter("enterpriseCode", enterpriseCode.ToString());
            sql.AddParameter("nameFragment", nameFragment);

            return _repository.RawSqlFetchList<Forestry>(sql);
        }

        public void UpdateFullName(int forestryId, string fullName)
        {
            var sql = new RawSqlCommand(@"
                UPDATE forestries
                SET pavadinima_pilnas = @fullName
                WHERE id = @forestryId;
            ");

            sql.AddParameter("fullName", fullName);
            sql.AddParameter("forestryId", forestryId);

            _repository.RawSqlExecuteNonQuery(sql);
        }
    }
}
