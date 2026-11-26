using System.Net;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Identity;
using MongoDB.Driver;
using sophieBeautyApi.Models;
using sophieBeautyApi.RepositoryInterfaces;
using sophieBeautyApi.ServiceInterfaces;

namespace sophieBeautyApi.services
{
    public class categoryService: ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public categoryService(ICategoryRepository categoryRepository)
        {
            this._categoryRepository = categoryRepository;
        }


        public async Task<List<category>> getAll()
        {
            var categories = await _categoryRepository.GetAllAsync();

            return categories;
        }

        public async Task<category?> create(string name)
        {

            var all = await getAll();
            if (all.Any(cat => cat.name.ToLower() == name.ToLower()))
            {
                return null;
            }

            category categoryToAdd = new category(name);



            return await _categoryRepository.CreateAsync(categoryToAdd);

            
        }

        public async Task<bool> delete(category category)
        {
            var result = await _categoryRepository.DeleteAsync(category);

            return result;
        }
    }

}