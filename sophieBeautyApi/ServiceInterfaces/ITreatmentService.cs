

using sophieBeautyApi.Models;

namespace sophieBeautyApi.ServiceInterfaces
{
    public interface ITreatmentService
    {

        Task<IEnumerable<treatment>> getAll();
        

        Task<treatment> create(treatment newTreatment);
        

        Task<treatment?> getById(string id);
        

        Task<bool> update(treatment updatedTreatment);
        

        Task<bool> delete(string id);
        

        Task<IEnumerable<treatment>> getListByIds(List<string> ids);
        


    }
}