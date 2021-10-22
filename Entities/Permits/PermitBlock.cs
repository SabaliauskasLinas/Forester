using System.Collections.Generic;

namespace Entities.Permits
{
    public class PermitBlock
    {
        public int Id { get; set; }
        public int PermitId { get; set; }
        public int CadastralBlockId { get; set; }
        public bool HasUnmappedSites { get; set; }
        public List<PermitSite> PermitSites { get; set; }
    }
}
