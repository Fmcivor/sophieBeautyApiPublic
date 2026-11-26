using Microsoft.AspNetCore.Http.HttpResults;
using MongoDB.Driver;
using sophieBeautyApi.Models;
using sophieBeautyApi.RepositoryInterfaces;
using sophieBeautyApi.Repositorys;
using sophieBeautyApi.ServiceInterfaces;

namespace sophieBeautyApi.services
{
    public class treatmentService: ITreatmentService 
    {
        

        private readonly ITreatmentRepository _treatmentRepository;

        public treatmentService(ITreatmentRepository treatmentRepo)
        {
            _treatmentRepository = treatmentRepo;
        }

        public async Task<IEnumerable<treatment>> getAll()
        {
            var treatments = await _treatmentRepository.GetAllAsync();

            return treatments;
        }

        public async Task<treatment> create(treatment newTreatment)
        {
            await _treatmentRepository.CreateAsync(newTreatment);

            return newTreatment;
        }

        public async Task<treatment?> getById(string id)
        {
            return await _treatmentRepository.GetByIdAsync(id);
        }

        public async Task<bool> update(treatment updatedTreatment)
        {
            var result = await _treatmentRepository.UpdateAsync(updatedTreatment);
            return result;
        }

        public async Task<bool> delete(string id)
        {
            var result = await _treatmentRepository.DeleteAsync(id);
            return result;
        }
    
        public async Task<IEnumerable<treatment>> getListByIds(List<string> ids)
        {

            var treatments = await _treatmentRepository.GetListByIdsAsync(ids);
            return treatments;
        }
    
    }
}