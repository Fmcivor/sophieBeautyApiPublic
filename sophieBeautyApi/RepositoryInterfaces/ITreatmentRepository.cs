using sophieBeautyApi.Models;

namespace sophieBeautyApi.RepositoryInterfaces
{
    public interface ITreatmentRepository
    {
        Task<IEnumerable<treatment>> GetAllAsync();
        Task<treatment> CreateAsync(treatment newTreatment);
        Task<treatment?> GetByIdAsync(string id);
        Task<bool> UpdateAsync(treatment updatedTreatment);
        Task<bool> DeleteAsync(string id);
        Task<IEnumerable<treatment>> GetListByIdsAsync(List<string> ids);
    }
}
