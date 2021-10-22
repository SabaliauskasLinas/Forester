using System.Collections.Generic;

namespace Entities.Permits
{
    public class PermitSite
    {
        public int Id { get; set; }
        public int PermitBlockId { get; set; }
        public int? CadastralSiteId { get; set; }
        public List<string> SiteCodes { get; set; }
        public string SiteCodesString { get; set; }
        public decimal Area { get; set; }
    }
}
