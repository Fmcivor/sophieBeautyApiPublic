using sophieBeautyApi.Models;

namespace sophieBeautyApi.ServiceInterfaces
{
    public interface IAvailabilitySlotService
    {
        Task<IEnumerable<availablilitySlot>> getAll();

        Task<availablilitySlot?> create(availablilitySlot newSlot);

        Task<bool> bookingWithinAvailabilitySlot(DateTime apptTime, int duration);

        Task<bool> delete(availablilitySlot slot);

        Task<IEnumerable<TimeSpan>> getAvailableTimes(availableTimesRequest request);

        Task<IEnumerable<availablilitySlot>> getSlotsByDate(DateTime date);

        Task<bool> deleteAll();
    }
}