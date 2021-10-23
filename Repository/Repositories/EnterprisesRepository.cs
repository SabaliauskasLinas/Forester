using Common;
using Entities.Cadastre;
using Entities.Repository;
using System.Collections.Generic;

namespace Repository.Repositories
{
    public class EnterprisesRepository : IEnterprisesRepository
    {
        private readonly IDbRepository _repository;

        public EnterprisesRepository(IDbRepository repository)
        {
            _repository = repository;
        }

        public string GetFullName(int cadastralEnterpriseId)
        {
            var sql = new RawSqlCommand(@"
                SELECT pavadinima_pilnas
                FROM enterprises
                WHERE id = @cadastralEnterpriseId;
            ");

            sql.AddParameter("cadastralEnterpriseId", cadastralEnterpriseId);

            return _repository.RawSqlExecuteScalar(sql).ChangeType<string>();
        }

        public Enterprise GetEnterpriseByFullName(string fullName)
        {
            var sql = new RawSqlCommand(@"
                SELECT id, mu_kod AS Code, pavadinima AS Name, pavadinima_pilnas AS FullName
                FROM enterprises
                WHERE pavadinima_pilnas = @fullName;
            ");

            sql.AddParameter("fullName", fullName);

            return _repository.RawSqlFetchSingle<Enterprise>(sql);
        }

        public List<Enterprise> GetEnterprisesByNameFragment(string nameFragment)
        {
            var sql = new RawSqlCommand(@"
                SELECT id, mu_kod AS Code, pavadinima AS Name, pavadinima_pilnas AS FullName
                FROM enterprises
                WHERE pavadinima LIKE CONCAT('%', @nameFragment, '%');
            ");

            sql.AddParameter("nameFragment", nameFragment);

            return _repository.RawSqlFetchList<Enterprise>(sql);
        }

        public void UpdateFullName(int enterpriseId, string fullName)
        {
            var sql = new RawSqlCommand(@"
                UPDATE enterprises
                SET pavadinima_pilnas = @fullName
                WHERE id = @enterpriseId;
            ");

            sql.AddParameter("fullName", fullName);
            sql.AddParameter("enterpriseId", enterpriseId);

            _repository.RawSqlExecuteNonQuery(sql);
        }
    }
}
