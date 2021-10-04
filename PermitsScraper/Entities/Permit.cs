using System;
using System.Collections.Generic;
using System.Text;

namespace PermitsScraper.Entities
{
    public class Permit
    {
        public string Number { get; set; }
        public string Region { get; set; }
        public string District { get; set; }
        public string OwnershipForm { get; set; }
        public string Enterprise { get; set; }
        public string Forestry { get; set; }
        public string Square { get; set; }
        public string Plots { get; set; }
        public string Area { get; set; }
        public string CadastralLocation { get; set; }
        public string CadastralBlock { get; set; }
        public string CadastralNumber { get; set; }
        public string CuttingType { get; set; }
        public string ValidFrom { get; set; }
        public string ValidTo { get; set; }
    }
}
