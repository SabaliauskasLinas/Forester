using System;
using System.Collections.Generic;
using System.Text;

namespace Entities.Import
{
    public class PermitSite
    {
        public int PermitBlockId { get; set; }
        public int? CadastralSideId { get; set; }
        public List<string> SiteCodes { get; set; }
        public decimal Area { get; set; }
    }
}
