using sophieBeautyApi.Models;

namespace sophieBeautyApi.RepositoryInterfaces
{
    public interface IAdminRepository
    {
        Task<admin> RegisterAsync(admin admin);
        Task<admin?> findAdminByUsername(string username);
    }
}
