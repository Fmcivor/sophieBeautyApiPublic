using sophieBeautyApi.Models;

namespace sophieBeautyApi.RepositoryInterfaces
{
    public interface ICategoryRepository
    {
        Task<List<category>> GetAllAsync();
        Task<category> CreateAsync(category c);
        Task<bool> DeleteAsync(category category);
    }
}
