namespace Repository.Repositories
{
    public interface ISitesRepository
    {
        int? GetSiteId(int enterpriseCode, int forestryCode, string block, string site);
    }
}