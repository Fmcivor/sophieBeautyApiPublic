using sophieBeautyApi.Models;

namespace sophieBeautyApi.ServiceInterfaces
{
    public interface IEmailService
    {
        Task Send(booking newBooking);

        Task sendCancellation(booking cancelledBooking);

        Task sendReminder(booking booking);

        Task notifyNewBooking(booking newBooking);
    }
}