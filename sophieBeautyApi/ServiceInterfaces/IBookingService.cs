using sophieBeautyApi.Models;

namespace sophieBeautyApi.ServiceInterfaces
{
    public interface IBookingService
    {
        Task<IEnumerable<booking>> getAll();

        Task<BookingResult> create(newBookingDTO newBooking);

        Task<booking?> getById(string id);

        Task<bool> update(booking updatedBooking);

        Task<bool> markReminderSent(booking updatedBooking);

        Task<BookingResult> delete(string id);

        Task<IEnumerable<booking>> getTodaysBooking(DateTime date);

        Task<IEnumerable<booking>> getUpcomingBookings(DateTime date);

        Task<IEnumerable<booking>> getNextDayBookings(DateTime date);

        Task<int> getWeeklyRevenue(DateTime date);

        Task<int> getMonthlyRevenue(DateTime date);
    }
}