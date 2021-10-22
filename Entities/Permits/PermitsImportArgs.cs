using Entities.Scraping;
using System.Collections.Generic;

namespace Entities.Permits
{
    public class PermitsImportArgs
    {
        public string Enterprise { get; set; }
        public string Forestry { get; set; }
        public List<ScrapedPermit> ScrapedPermits { get; set; }
    }
}
