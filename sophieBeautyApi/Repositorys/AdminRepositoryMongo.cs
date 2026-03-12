using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using MongoDB.Driver;
using sophieBeautyApi.Models;
using sophieBeautyApi.RepositoryInterfaces;

namespace sophieBeautyApi.Repositorys
{
    public class AdminRepositoryMongo : IAdminRepository
    {
        private readonly IMongoCollection<admin> _adminTable;
        private readonly MongoClient _mongoClient;

        public AdminRepositoryMongo(MongoClient mongoClient)
        {
            _mongoClient = mongoClient;
            var database = _mongoClient.GetDatabase("SophieBeauty");
            _adminTable = database.GetCollection<admin>("admins");
        }

        public async Task<admin> RegisterAsync(admin admin)
        {
            await _adminTable.InsertOneAsync(admin);
            return admin;
        }

        public async Task<admin?> findAdminByUsername(string username)
        {
            var account = await _adminTable.Find(a => a.username == username).FirstOrDefaultAsync();

            return account;
        }

        
    }
}
