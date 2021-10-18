namespace Repository.Repositories
{
    public interface IBlocksRepository
    {
        int? GetBlockId(int enterpriseCode, int forestryCode, string block);
    }
}