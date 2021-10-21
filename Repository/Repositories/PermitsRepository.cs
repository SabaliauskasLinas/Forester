using Common;
using Entities.Cadastre;
using Entities.Import;
using Entities.Repository;
using System;
using System.Collections.Generic;

namespace Repository.Repositories
{
    public class PermitsRepository : IPermitsRepository
    {
        private readonly IDbRepository _repository;

        public PermitsRepository(IDbRepository repository)
        {
            _repository = repository;
        }

        public int InsertPermit(Permit permit)
        {
            var sql = new RawSqlCommand(@"
                INSERT INTO permits (permit_number, region, district, ownership_form, cadastral_enterprise_id, cadastral_forestry_id, cadastral_location, cadastral_block, cadastral_number, cutting_type, valid_from, valid_to) 
                VALUES (@permitNumber, @region, @district, @ownershipForm, @cadastralEnterpriseId, @cadastralForestryId, @cadastralLocation, @cadastralBlock, @cadastralNumber, @cuttingType, @validFrom, @validTo) 
                RETURNING id;
            ");

            sql.AddParameter("permitNumber", permit.PermitNumber);
            sql.AddParameter("region", permit.Region);
            sql.AddParameter("district", permit.District);
            sql.AddParameter("ownershipForm", permit.OwnershipForm);
            sql.AddParameter("cadastralEnterpriseId", permit.CadastralEnterpriseId);
            sql.AddParameter("cadastralForestryId", permit.CadastralForestryId);
            sql.AddParameter("cadastralLocation", permit.CadastralLocation);
            sql.AddParameter("cadastralBlock", permit.CadastralBlock);
            sql.AddParameter("cadastralNumber", permit.CadastralNumber);
            sql.AddParameter("cuttingType", permit.CadastralNumber);
            sql.AddParameter("validFrom", permit.ValidFrom);
            sql.AddParameter("validTo", permit.ValidTo);

            return _repository.RawSqlExecuteScalar(sql).ChangeType<int>();
        }

        public int InsertPermitBlock(PermitBlock permitBlock)
        {
            var sql = new RawSqlCommand(@"
                INSERT INTO permits_blocks (permit_id, cadastral_block_id) 
                VALUES (@permitId, @cadastralBlockId) 
                RETURNING id;
            ");

            sql.AddParameter("permitId", permitBlock.PermitId);
            sql.AddParameter("cadastralBlockId", permitBlock.CadastralBlockId);

            return _repository.RawSqlExecuteScalar(sql).ChangeType<int>();
        }

        public int InsertPermitSite(PermitSite permitSite)
        {
            var sql = new RawSqlCommand(@"
                INSERT INTO permits_sites (permit_block_id, cadastral_site_id, site_codes, area) 
                VALUES (@permitBlockId, @cadastralSiteId, @siteCodes, @area) 
                RETURNING id;
            ");

            sql.AddParameter("permitBlockId", permitSite.PermitBlockId);
            sql.AddParameter("cadastralSiteId", (object)permitSite.CadastralSideId);
            sql.AddParameter("siteCodes", permitSite.SiteCodes);
            sql.AddParameter("area", permitSite.Area);

            return _repository.RawSqlExecuteScalar(sql).ChangeType<int>();
        }

        public void UpdateBlockHasUnmappedSite(int permitBlockId, bool hasUnmappedSite)
        {
            var sql = new RawSqlCommand(@"
                UPDATE permits_blocks
                SET has_unmapped_sites = @hasUnmappedSite
                WHERE id = @id;
            ");

            sql.AddParameter("id", permitBlockId);
            sql.AddParameter("hasUnmappedSite", hasUnmappedSite);

            _repository.RawSqlExecuteNonQuery(sql);
        }
    }
}
