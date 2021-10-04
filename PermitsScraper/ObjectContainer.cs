using SimpleInjector;
using System;
using System.Collections.Generic;
using System.Text;

namespace PermitsScraper
{
    internal static class ObjectContainer
    {
        public static void Init()
        {
            var container = new Container();
            Repository.ObjectContainerInitializer.Init(container);
        }

        public static T GetInstance<T>() where T : class
        {
            return Repository.ObjectContainerInitializer.Container.GetInstance<T>();
        }

        public static Scope BeginContext => SimpleInjector.Lifestyles.AsyncScopedLifestyle.BeginScope(Repository.ObjectContainerInitializer.Container);
    }
}
