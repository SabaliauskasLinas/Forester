using Microsoft.Extensions.Configuration;
using PermitsScraper.Services;
using SimpleInjector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PermitsScraper
{
    internal static class ObjectContainer
    {
        public static void Init()
        {
            var container = new Container();

            var configuration = new ConfigurationBuilder()
               .SetBasePath(Directory.GetParent(AppContext.BaseDirectory).FullName)
               .AddJsonFile("appsettings.json", false)
               .Build();

            container.Register(() => configuration, Lifestyle.Singleton);

            container.Register<IScrapingService, ScrapingService>(Lifestyle.Singleton);
            container.Register<IScrapingClientService, ScrapingClientService>(Lifestyle.Singleton);
            container.Register<IPermitsImportService, PermitsImportService>(Lifestyle.Singleton);
            Repository.ObjectContainerInitializer.Init(container);
        }

        public static T GetInstance<T>() where T : class
        {
            return Repository.ObjectContainerInitializer.Container.GetInstance<T>();
        }

        public static Scope BeginContext => SimpleInjector.Lifestyles.AsyncScopedLifestyle.BeginScope(Repository.ObjectContainerInitializer.Container);
    }
}
