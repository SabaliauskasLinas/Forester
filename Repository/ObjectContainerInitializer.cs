using Repository.Repositories;
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.Text;

namespace Repository
{
    public static class ObjectContainerInitializer
    {
        public static Container Container { get; private set; }
        public static void Init(Container container)
        {
            Container = container;

            Container.Register<IDbRepository, DbRepository>(Lifestyle.Singleton);
            Container.Register<ITestRepo, TestRepo>(Lifestyle.Singleton);
        }
    }
}
