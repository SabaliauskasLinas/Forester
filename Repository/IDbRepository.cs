using Entities.Repository;
using System;
using System.Collections.Generic;
using System.Text;

namespace Repository
{
    public interface IDbRepository : IDisposable
    {
        List<TR> RawSqlFetchList<TR>(RawSqlCommand command) where TR : class;
        List<TR> RawSqlFetchSingleColumnList<TR>(RawSqlCommand command);
        TR RawSqlFetchSingle<TR>(RawSqlCommand command) where TR : class;
        Dictionary<string, object> RawSqlFetchSingleRow(RawSqlCommand command);
        object RawSqlExecuteScalar(RawSqlCommand command);
        int RawSqlExecuteNonQuery(RawSqlCommand command);
    }
}
