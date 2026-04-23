using MongoDB.Driver;
using sophieBeautyApi.Models;
using sophieBeautyApi.RepositoryInterfaces;

namespace sophieBeautyApi.Repositorys
{
    public class CategoryRepositoryMongo : ICategoryRepository
    {
        private MongoClient _mongoClient;
        private IMongoCollection<category> categoryTable;

        public CategoryRepositoryMongo(MongoClient mongoClient)
        {
            _mongoClient = mongoClient;
            var database = _mongoClient.GetDatabase("SophieBeauty");
            categoryTable = database.GetCollection<category>("categories");
        }

        public async Task<List<category>> GetAllAsync()
        {
            var categories = await categoryTable.Find(c => true).ToListAsync();
            return categories;
        }

        public async Task<category> CreateAsync(category c)
        {
            await categoryTable.InsertOneAsync(c);
            return c;
        }

        public async Task<bool> DeleteAsync(category category)
        {
            var result = await categoryTable.DeleteOneAsync(c => c.Id == category.Id);
            return result.DeletedCount == 1;
        }
    }
}
