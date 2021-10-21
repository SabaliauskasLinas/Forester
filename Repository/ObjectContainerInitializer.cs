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
            Container.Register<IEnterprisesRepository, EnterprisesRepository>(Lifestyle.Singleton);
            Container.Register<IForestriesRepository, ForestriesRepository>(Lifestyle.Singleton);
            Container.Register<IBlocksRepository, BlocksRepository>(Lifestyle.Singleton);
            Container.Register<ISitesRepository, SitesRepository>(Lifestyle.Singleton);
            Container.Register<IPermitsRepository, PermitsRepository>(Lifestyle.Singleton);
        }
    }
}
