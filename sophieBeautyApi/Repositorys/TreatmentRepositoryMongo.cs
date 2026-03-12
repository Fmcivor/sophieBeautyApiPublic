using MongoDB.Driver;
using sophieBeautyApi.Models;
using sophieBeautyApi.RepositoryInterfaces;

namespace sophieBeautyApi.Repositorys
{
    public class TreatmentRepositoryMongo : ITreatmentRepository
    {
        private readonly IMongoCollection<treatment> treatmentTable;
        private readonly MongoClient _mongoClient;

        public TreatmentRepositoryMongo(MongoClient mongoClient)
        {
            _mongoClient = mongoClient;
            var database = _mongoClient.GetDatabase("SophieBeauty");
            treatmentTable = database.GetCollection<treatment>("services");
        }

        public async Task<IEnumerable<treatment>> GetAllAsync()
        {
            var treatments = await treatmentTable.Find(t => true).ToListAsync();
            return treatments;
        }

        public async Task<treatment> CreateAsync(treatment newTreatment)
        {
            await treatmentTable.InsertOneAsync(newTreatment);
            return newTreatment;
        }

        public async Task<treatment?> GetByIdAsync(string id)
        {
            var filter = Builders<treatment>.Filter.Eq(t => t.Id, id);
            return await treatmentTable.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateAsync(treatment updatedTreatment)
        {
            var filter = Builders<treatment>.Filter.Eq(t => t.Id, updatedTreatment.Id);
            var result = await treatmentTable.ReplaceOneAsync(filter, updatedTreatment);
            return result.MatchedCount > 0;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var filter = Builders<treatment>.Filter.Eq(t => t.Id, id);
            var result = await treatmentTable.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }

        public async Task<IEnumerable<treatment>> GetListByIdsAsync(List<string> ids)
        {
            var treatments = await treatmentTable.Find(t => t.Id !=null && ids.Contains(t.Id) == true).ToListAsync();
            return treatments;
        }
    }
}
