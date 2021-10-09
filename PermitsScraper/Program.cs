using Microsoft.Extensions.Configuration;
using PermitsScraper.Services;

namespace PermitsScraper
{
    public class Program
    {
        public static IConfigurationRoot configuration;
        public static void Main(string[] args)
        {
            ObjectContainer.Init();
            using (ObjectContainer.BeginContext)
            {
                ObjectContainer.GetInstance<IScrapingService>().Run();
            }
        }
    }
}
