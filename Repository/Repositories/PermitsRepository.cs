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
            sql.AddParameter("cuttingType", permit.CuttingType);
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

        public void UpdatePermit(Permit permit)
        {
            var sql = new RawSqlCommand(@"
                UPDATE permits
                SET 
                    region = @Region,
                    district = @District,
                    ownership_form = @OwnershipForm,
                    cadastral_enterprise_id = @CadastralEnterpriseId,
                    cadastral_forestry_id = @CadastralForestryId,
                    cadastral_location = @CadastralLocation,
                    cadastral_block = @CadastralBlock,
                    cadastral_number = @CadastralNumber,
                    cutting_type = @CuttingType,
                    valid_from = @ValidFrom,
                    valid_to = @ValidTo,
                    updated_at = CURRENT_TIMESTAMP
                WHERE id = @Id;
            ");

            sql.AddParameter("Id", permit.Id);
            sql.AddParameter("Region", permit.Region);
            sql.AddParameter("District", permit.District);
            sql.AddParameter("OwnershipForm", permit.OwnershipForm);
            sql.AddParameter("CadastralEnterpriseId", permit.CadastralEnterpriseId);
            sql.AddParameter("CadastralForestryId", permit.CadastralForestryId);
            sql.AddParameter("CadastralLocation", permit.CadastralLocation);
            sql.AddParameter("CadastralBlock", permit.CadastralBlock);
            sql.AddParameter("CadastralNumber", permit.CadastralNumber);
            sql.AddParameter("CuttingType", permit.CuttingType);
            sql.AddParameter("ValidFrom", permit.ValidFrom);
            sql.AddParameter("ValidTo", permit.ValidTo);

            _repository.RawSqlExecuteNonQuery(sql);
        }

        public void UpdatePermitBlock(PermitBlock permitBlock)
        {
            var sql = new RawSqlCommand(@"
                UPDATE permits_blocks
                SET 
                    has_unmapped_sites = @HasUnmappedSites,
                    updated_at = CURRENT_TIMESTAMP
                WHERE id = @Id;
            ");

            sql.AddParameter("Id", permitBlock.Id);
            sql.AddParameter("HasUnmappedSites", permitBlock.HasUnmappedSites);

            _repository.RawSqlExecuteNonQuery(sql);
        }

        public void UpdatePermitSite(PermitSite permitSite)
        {
            var sql = new RawSqlCommand(@"
                UPDATE permits_sites
                SET
                    area = @Area,
                    site_codes = @SiteCodes,
                    updated_at = CURRENT_TIMESTAMP
                WHERE id = @Id;
            ");

            sql.AddParameter("Id", permitSite.Id);
            sql.AddParameter("Area", permitSite.Area);
            sql.AddParameter("SiteCodes", string.Join(",", permitSite.SiteCodes));

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

        public void DeletePermitSites(int permitBlockId)
        {
            var sql = new RawSqlCommand($@"
                DELETE FROM permits_sites
                WHERE permit_block_id = @PermitBlockId;
            ");

            sql.AddParameter("PermitBlockId", permitBlockId);

            _repository.RawSqlExecuteNonQuery(sql);
        }

        public void DeletePermitSites(List<int> ids)
        {
            var sql = new RawSqlCommand($@"
                DELETE FROM permits_sites
                WHERE id IN ({string.Join(",", ids)});
            ");

            _repository.RawSqlExecuteNonQuery(sql);
        }

        public void DeletePermitBlock(int id)
        {
            var sql = new RawSqlCommand($@"
                DELETE FROM permits_blocks
                WHERE id = @Id;
            ");

            sql.AddParameter("Id", id);

            _repository.RawSqlExecuteNonQuery(sql);
        }

        public void InsertPermitHistory(int permitId, string change) => InsertPermitHistory(permitId, new List<string> { change });

        public void InsertPermitHistory(int permitId, List<string> changes)
        {
            var sql = new RawSqlCommand("INSERT INTO permits_history (permit_id, change) VALUES ");

            sql.AddParameter("permitId", permitId);

            var counter = 0;
            foreach (var change in changes)
            {
                if (change == changes.Last())
                    sql.CommandText += $"(@permitId, @change{counter});";
                else
                    sql.CommandText += $"(@permitId, @change{counter}),";

                sql.AddParameter($"change{counter}", change);
                counter++;
            }

            _repository.RawSqlExecuteScalar(sql).ChangeType<int>();
        }
    }
}
