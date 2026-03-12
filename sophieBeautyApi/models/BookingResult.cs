
namespace sophieBeautyApi.Models
{
    public class BookingResult
    {
        public booking? Booking { get; }
        public string? Error { get; }
        public bool IsSuccess {get; }

        // Success
        public BookingResult(booking booking)
        {
            Booking = booking;
            IsSuccess = true;
        }

        // Failure
        public BookingResult(string error)
        {
            Error = error;
            IsSuccess = false;
        }
    }
}