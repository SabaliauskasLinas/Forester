using System;
using System.Collections.Generic;
using System.Text;

namespace Entities.Permits
{
    public class PermitsImportResult
    {
        public int TotalPermitsInserted { get; set; }
        public int TotalPermitsUpdated { get; set; }
        public int TotalPermitsPartiallyFailed { get; set; }
        public int TotalPermitsFailed { get; set; }
    }
}
