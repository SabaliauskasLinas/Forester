using Entities.Repository;
using System.Collections.Generic;

namespace Repository.Repositories
{
    public interface ITestRepo
    {
        void TestDatabase();
        List<TestObject> TestDatabase2();
    }
}