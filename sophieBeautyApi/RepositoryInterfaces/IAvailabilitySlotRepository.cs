using sophieBeautyApi.Models;

namespace sophieBeautyApi.RepositoryInterfaces
{
    public interface IAvailabilitySlotRepository
    {
        Task<IEnumerable<availablilitySlot>> GetAllAsync();
        Task<availablilitySlot> CreateAsync(availablilitySlot newSlot);
        Task<bool> BookingWithinAvailabilitySlotAsync(DateTime apptTime);
        Task<bool> DeleteAsync(availablilitySlot slot);
        Task<IEnumerable<availablilitySlot>> GetSlotsByDateAsync(DateTime date);
        Task<bool> DeleteAllAsync();
    }
}
