using System.Collections.Generic;

namespace Entities.Scraping
{
    public class PermitsImportArgs
    {
        public string Enterprise { get; set; }
        public string Forestry { get; set; }
        public List<Permit> Permits { get; set; }
    }
}
