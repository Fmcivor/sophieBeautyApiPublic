using sophieBeautyApi.Models;

namespace sophieBeautyApi.ServiceInterfaces
{
    public interface ICategoryService
    {
        Task<List<category>> getAll();

        Task<category?> create(string name);

        Task<bool> delete(category category);
    }
}