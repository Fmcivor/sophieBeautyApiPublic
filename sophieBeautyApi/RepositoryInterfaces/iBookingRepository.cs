using sophieBeautyApi.Models;

namespace sophieBeautyApi.RepositoryInterfaces
{
    public interface IBookingRepository
    {
        Task<IEnumerable<booking>> GetAllAsync();
        Task<booking> CreateAsync(booking newBooking);
        Task<booking?> GetByIdAsync(string id);
        Task<bool> UpdateAsync(booking updatedBooking);
        Task<bool> MarkReminderSentAsync(booking updatedBooking);
        Task<bool> DeleteAsync(string id);
        Task<IEnumerable<booking>> GetBookingsByDateAsync(DateTime start,DateTime end);
        Task<booking?> GetBookingOnDateAsync(DateTime date);
        Task<IEnumerable<booking>> GetTodaysBookingAsync(DateTime start, DateTime end);
        Task<IEnumerable<booking>> GetUpcomingBookingsAsync(DateTime start);
        Task<IEnumerable<booking>> GetNextDayBookingsAsync(DateTime start, DateTime end);
        Task<IEnumerable<booking>> getBookingsByDateRange(DateTime start, DateTime end, booking.status status);
        Task<IEnumerable<booking>> GetExpiredBookingsAsync(DateTime utcNow);
    }
}
