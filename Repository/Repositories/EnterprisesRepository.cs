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

        public List<Enterprise> GetEnterprisesByNameFragment(string nameFragment)
        {
            var sql = new RawSqlCommand(@"
                SELECT id, mu_kod AS Code, pavadinima AS Name
                FROM enterprises
                WHERE pavadinima LIKE CONCAT('%', @nameFragment, '%');
            ");

            sql.AddParameter("nameFragment", nameFragment);

            return _repository.RawSqlFetchList<Enterprise>(sql);
        }
    }
}
