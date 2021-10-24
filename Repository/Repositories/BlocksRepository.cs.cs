using Common;
using Entities.Cadastre;
using Entities.Repository;
using System.Collections.Generic;

namespace Repository.Repositories
{
    public class BlocksRepository : IBlocksRepository
    {
        private readonly IDbRepository _repository;

        public BlocksRepository(IDbRepository repository)
        {
            _repository = repository;
        }

        public int? GetBlockId(int enterpriseCode, int forestryCode, string block)
        {
            var sql = new RawSqlCommand(@"
                SELECT id
                FROM blocks
                WHERE mu_kod = @enterpriseCode AND gir_kod = @forestryCode AND kv_nr = @block;
            ");

            sql.AddParameter("enterpriseCode", enterpriseCode.ToString());
            sql.AddParameter("forestryCode", forestryCode.ToString());
            sql.AddParameter("block", block);

            return _repository.RawSqlExecuteScalar(sql).ChangeType<int?>();
        }

        public string GetBlockNumberById(int cadastralBlockId)
        {
            var sql = new RawSqlCommand(@"
                SELECT kv_nr
                FROM blocks
                WHERE id = @cadastralBlockId;
            ");

            sql.AddParameter("cadastralBlockId", cadastralBlockId);

            return _repository.RawSqlExecuteScalar(sql).ChangeType<string>();
        }
    }
}
