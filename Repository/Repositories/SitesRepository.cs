using Common;
using Entities.Cadastre;
using Entities.Repository;
using System.Collections.Generic;

namespace Repository.Repositories
{
    public class SitesRepository : ISitesRepository
    {
        private readonly IDbRepository _repository;

        public SitesRepository(IDbRepository repository)
        {
            _repository = repository;
        }

        public int? GetSiteId(int enterpriseCode, int forestryCode, string block, string site)
        {
            var sql = new RawSqlCommand(@"
                SELECT id
                FROM sites
                WHERE mu_kod = @enterpriseCode AND gir_kod = @forestryCode AND kv_nr = @block AND skl_nr = @site;
            ");

            sql.AddParameter("enterpriseCode", enterpriseCode.ToString());
            sql.AddParameter("forestryCode", forestryCode.ToString());
            sql.AddParameter("block", block);
            sql.AddParameter("site", site);

            return _repository.RawSqlExecuteScalar(sql).ChangeType<int?>();
        }
    }
}
