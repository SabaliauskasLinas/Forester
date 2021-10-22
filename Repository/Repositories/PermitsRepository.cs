using Common;
using Entities.Cadastre;
using Entities.Permits;
using Entities.Repository;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

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
            sql.AddParameter("cadastralSiteId", permitSite.CadastralSiteId);
            sql.AddParameter("siteCodes", string.Join(",", permitSite.SiteCodes));
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

        public Permit GetFullPermit(string permitNumber)
        {
            var sqlPermit = new RawSqlCommand(@"
                SELECT Id, permit_number AS PermitNumber, region, district, ownership_form AS OwnershipForm, cadastral_enterprise_id AS CadastralEnterpriseId, 
                    cadastral_forestry_id AS CadastralForestryId, cadastral_location AS CadastralLocation, cadastral_block AS CadastralBlock, cadastral_number AS CadastralNumber,
                    cutting_type AS CuttingType, valid_from AS ValidFrom, valid_to AS ValidTo
                FROM permits p
                WHERE p.permit_number = @permitNumber;
            ");

            sqlPermit.AddParameter("permitNumber", permitNumber);

            var permit = _repository.RawSqlFetchSingle<Permit>(sqlPermit);

            if (permit == null)
                return null;

            var sqlPermitBlocks = new RawSqlCommand(@"
                SELECT id, permit_id AS PermitId, cadastral_block_id AS CadastralBlockId, has_unmapped_sites AS HasUnmappedSites
                FROM permits_blocks pb
                WHERE pb.permit_id = @permitId;
            ");

            sqlPermitBlocks.AddParameter("permitId", permit.Id);

            permit.PermitBlocks = _repository.RawSqlFetchList<PermitBlock>(sqlPermitBlocks);

            var sqlPermitSites = new RawSqlCommand(@"
                SELECT id, permit_block_id AS PermitBlockId, cadastral_site_id AS CadastralSiteId, site_codes AS SiteCodesString, area
                FROM permits_sites ps
                WHERE ps.permit_block_id = @permitBlockId;
            ");

            foreach (var permitBlock in permit.PermitBlocks)
            {
                sqlPermitSites.Parameters["permitBlockId"] = new RawSqlParameter("permitBlockId", permitBlock.Id);
                permitBlock.PermitSites = _repository.RawSqlFetchList<PermitSite>(sqlPermitSites);
                permitBlock.PermitSites.ForEach(ps => ps.SiteCodes = ps.SiteCodesString.Split(",").ToList());
            }

            return permit;
        }

        public void DeletePermitSites(List<int> ids)
        {
            var sql = new RawSqlCommand($@"
                DELETE FROM permits_sites
                WHERE id IN ({string.Join(",", ids)});
            ");

            _repository.RawSqlExecuteNonQuery(sql);
        }
    }
}
