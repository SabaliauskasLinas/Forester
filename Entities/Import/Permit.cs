using System;

namespace Entities.Import
{
    public class Permit
    {
        public int Id { get; set; }
        public string PermitNumber { get; set; }
        public string Region { get; set; }
        public string District { get; set; }
        public string OwnershipForm { get; set; }
        public int CadastralEnterpriseId { get; set; }
        public int CadastralForestryId { get; set; }
        public string CadastralLocation { get; set; }
        public string CadastralBlock { get; set; }
        public string CadastralNumber { get; set; }
        public string CuttingType { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
    }
}
