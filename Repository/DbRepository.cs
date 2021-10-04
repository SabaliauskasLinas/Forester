using Common;
using Entities.Repository;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Reflection;
using System.Text;

namespace Repository
{
    public class DbRepository : IDbRepository
    {
        private NpgsqlConnection _conn;
        private NpgsqlTransaction _transaction;
        public bool HasActiveTransaction { get { return _runInTransaction; } }
        public DbConnection Connection { get { return GetConnection(); } }
        public bool _runInTransaction { get; set; }
        public DbTransaction Transaction
        {
            get
            {
                return GetTransaction();
            }
        }

        private NpgsqlTransaction GetTransaction()
        {
            if (_runInTransaction && _transaction == null)
            {
                _transaction = GetConnection().BeginTransaction();
            }
            return _transaction;
        }

        #region Raw SQL Commands

        public List<TR> RawSqlFetchList<TR>(RawSqlCommand command) where TR : class
        {
            var list = new List<TR>();
            using (var cmd = CreateCommand(command))
            {
                using (var rd = cmd.ExecuteReader())
                {
                    var settings = GetConversionSettings<TR>();
                    while (rd.Read())
                    {
                        var newEntity = ReadEntity<TR>(rd, settings);
                        list.Add(newEntity);
                    }
                }
            }
            return list;
        }

        public List<TR> RawSqlFetchSingleColumnList<TR>(RawSqlCommand command)
        {
            var list = new List<TR>();
            using (var cmd = CreateCommand(command))
            {
                using (var rd = cmd.ExecuteReader())
                {
                    while (rd.Read())
                    {
                        object newEntity = rd[0].ChangeType<TR>();
                        list.Add((TR)newEntity);
                    }
                }
            }
            return list;
        }

        public TR RawSqlFetchSingle<TR>(RawSqlCommand command) where TR : class
        {
            TR item = null;
            using (var cmd = CreateCommand(command))
            {
                using (var rd = cmd.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        var settings = GetConversionSettings<TR>();
                        item = ReadEntity<TR>(rd, settings);
                    }
                }
            }
            return item;
        }

        public object RawSqlExecuteScalar(RawSqlCommand command)
        {
            object res;
            using (var cmd = CreateCommand(command))
            {
                res = cmd.ExecuteScalar();
            }
            return res;
        }

        public int RawSqlExecuteNonQuery(RawSqlCommand command)
        {
            int res;
            using (var cmd = CreateCommand(command))
            {
                res = cmd.ExecuteNonQuery();
            }
            return res;
        }

        public Dictionary<string, object> RawSqlFetchSingleRow(RawSqlCommand command)
        {
            var dict = new Dictionary<string, object>();
            using (var cmd = CreateCommand(command))
            {
                using (var rd = cmd.ExecuteReader())
                {
                    if (rd.Read())
                    {
                        for (var i = 0; i < rd.FieldCount; i++)
                        {
                            var name = rd.GetName(i);
                            var value = rd.IsDBNull(i) ? null : rd.GetValue(i);
                            dict.Add(name, value);
                        }
                    }
                }
            }
            return dict;
        }

        #endregion

        #region Command execution

        private NpgsqlCommand CreateCommand(RawSqlCommand command)
        {
            var conn = GetConnection();
            var cmd = conn.CreateCommand();
            cmd.CommandType = command.CommandType;
            cmd.CommandTimeout = command.CommandTimeout;
            cmd.CommandText = command.CommandText;

            //_log.Data("SQL EXEC[{0}]: {1}", _type, cmd.CommandText);
            //_log.Data($"{string.Join("\r\n", command.Parameters.Values.Select(a => $"  -- {a.Name} [{a.Value}]"))}");

            cmd.Transaction = GetTransaction();

            foreach (var param in command.Parameters.Keys)
            {
                var a = command.Parameters[param];
                cmd.Parameters.AddWithValue(a.Name, a.Value);
            }
            return cmd;
        }

        private Dictionary<string, EntityConversionSettings> GetConversionSettings<TR>() where TR : class
        {
            var settings = new Dictionary<string, EntityConversionSettings>(StringComparer.InvariantCultureIgnoreCase);
            var properties = typeof(TR).GetProperties();
            foreach (PropertyInfo p in properties)
            {
                if (p.CanWrite)
                {
                    settings.Add(p.Name, new EntityConversionSettings
                    {
                        PropertyInfo = p,
                        PropertyName = p.Name,
                        PropertyType = p.PropertyType,
                    });
                }
            }
            return settings;
        }

        private TR ReadEntity<TR>(IDataReader rd, Dictionary<string, EntityConversionSettings> settings) where TR : class
        {
            var newEntity = Activator.CreateInstance(typeof(TR), new object[0]) as TR;
            for (var i = 0; i < rd.FieldCount; i++)
            {
                if (settings.TryGetValue(rd.GetName(i), out EntityConversionSettings p))
                {
                    object value = rd.GetValue(i).ChangeType(p.PropertyType);
                    p.PropertyInfo.SetValue(newEntity, value);
                }
            }
            return newEntity;
        }

        #endregion

        #region Transactions

        public void TransactionBegin()
        {
            if (_runInTransaction)
                throw new InvalidOperationException("Nested transactions are not supported");
            _runInTransaction = true;

            //_log.Data("{0} Repository transaction started", _type);
        }

        public void TransactionCommit()
        {

            if (_transaction != null)
            {
                _transaction.Commit();
            }
            _transaction = null;
            _runInTransaction = false;

            CloseConnection();
            //_log.Data("{0} Repository transaction commited", _type);
        }

        public void TransactionRollback()
        {
            try
            {
                if (_transaction != null)
                {
                    _transaction.Rollback();
                    //_log.Info("{0} Repository transaction rollbacked", _type);
                }
            }
            catch (Exception exc)
            {
                //_log.Error(exc.Message);
            }
            finally
            {
                _transaction = null;
                _runInTransaction = false;
            }
            CloseConnection();
        }

        #endregion

        #region Connection

        private NpgsqlConnection GetConnection()
        {
            if (_conn == null)
            {
                try
                {
                    _conn = new NpgsqlConnection("Host=localhost;Username=postgres;Password=l8k5j2123;Database=forester");
                    _conn.Open();
                    //_log.Debug("{0} Connection OPENED", _type);
                }
                catch (Exception ex)
                {
                    //_log.Error
                }
            }

            if (_conn.State == ConnectionState.Closed)
            {
                _conn.Open();
                //_log.Debug("{0} Connection OPENED", _type);
            }

            return _conn;
        }

        private void CloseConnection()
        {
            if (_conn == null) return;
            _conn.Close();
            _conn.Dispose();
            _conn = null;
            //_log.Debug("{0} Connection CLOSED", _type);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            TransactionRollback();
            CloseConnection();
        }

        #endregion
    }
}
