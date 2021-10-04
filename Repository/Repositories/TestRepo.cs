using Common;
using Entities.Repository;
using System;
using System.Collections.Generic;
using System.Text;

namespace Repository.Repositories
{
    public class TestRepo : ITestRepo
    {
        private readonly IDbRepository _repository;
        public TestRepo(IDbRepository repository)
        {
            _repository = repository;
        }

        public void TestDatabase()
        {
            var sql = new RawSqlCommand("SELECT pavadinima FROM forestries WHERE pavadinima LIKE CONCAT('%', @xxx, '%');");

            sql.AddParameter("xxx", "as");

            var x = _repository.RawSqlFetchSingleColumnList<string>(sql);
        }

        public List<TestObject> TestDatabase2()
        {
            var sql = new RawSqlCommand("SELECT Id, Pavadinima FROM forestries WHERE pavadinima LIKE CONCAT('%', @xxx, '%');");

            sql.AddParameter("xxx", "as");

            var x = _repository.RawSqlFetchList<TestObject>(sql);

            return x;
        }
    }
}
