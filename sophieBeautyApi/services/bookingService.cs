using Microsoft.AspNetCore.Http.HttpResults;
using MongoDB.Driver;
using sophieBeautyApi.Models;
using sophieBeautyApi.RepositoryInterfaces;
using sophieBeautyApi.ServiceInterfaces;

namespace sophieBeautyApi.services
{
    public class bookingService : IBookingService
    {
        private readonly IBookingRepository _bookingRepository;

        private readonly IAvailabilitySlotService _availabilityService;
        private readonly ITreatmentService _treatmentService;
        private readonly IEmailService _emailService;

        public bookingService(IBookingRepository bookingRepository, IAvailabilitySlotService availablilitySlotService, ITreatmentService treatmentService,
        IEmailService emailService)
        {
            this._bookingRepository = bookingRepository;
            this._availabilityService = availablilitySlotService;
            this._treatmentService = treatmentService;
            this._emailService = emailService;
        }

        public async Task<IEnumerable<booking>> getAll()
        {
            var bookings = await _bookingRepository.GetAllAsync();


            var ukZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");
            foreach (var b in bookings)
            {
                b.appointmentDate = TimeZoneInfo.ConvertTimeFromUtc(b.appointmentDate, ukZone);
            }

            return bookings;
        }

        public async Task<BookingResult> create(newBookingDTO newBooking)
        {
            var treatments = await _treatmentService.getListByIds(newBooking.treatmentIds);

            List<string> treatmentNames = new List<string>();
            int duration = 0;
            int price = 0;

            foreach (var t in treatments)
            {
                treatmentNames.Add(t.name);
                duration += t.duration;
                price += t.price;
            }

            duration = (int)(Math.Ceiling(duration / 30.0) * 30);

            bool paid = false;
            if (newBooking.payByCard)
            {
                paid = true;
            }

            booking booking = new booking(newBooking.customerName, newBooking.appointmentDate, newBooking.email, treatmentNames, price,duration, newBooking.payByCard, paid, booking.status.DepositPending,newBooking.phoneNumber);

        

            bool withinAvailableTimeSlot = await _availabilityService.bookingWithinAvailabilitySlot(booking.appointmentDate, booking.duration);

            if (!withinAvailableTimeSlot)
            {
                return new BookingResult("TAKEN");
            }

            try
            {
                var created = await _bookingRepository.CreateAsync(booking);

                // Null check
                if (created == null)
                {
                    return new BookingResult("SERVER_ERROR");
                }

                var ukZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

                created.appointmentDate = TimeZoneInfo.ConvertTimeFromUtc(created.appointmentDate, ukZone);

                // await _emailService.Send(created);

                return new BookingResult(created);
            }
            catch (Exception)
            {
                return new BookingResult("SERVER_ERROR");
            }
        }

        public async Task<BookingResult> createBookingAdmin(newBookingDTO newBooking)
        {
            var treatments = await _treatmentService.getListByIds(newBooking.treatmentIds);


            List<string> treatmentNames = new List<string>();
            int duration = 0;
            int price = 0;

            foreach (var t in treatments)
            {
                treatmentNames.Add(t.name);
                duration += t.duration;
                price += t.price;
            }

            duration = (int)(Math.Ceiling(duration / 30.0) * 30);

            bool paid = false;
            if (newBooking.payByCard)
            {
                paid = true;
            }

            booking booking = new booking(newBooking.customerName, newBooking.appointmentDate, newBooking.email, treatmentNames, price,duration, newBooking.payByCard, paid, booking.status.Confirmed,newBooking.phoneNumber);

        
            bool withinAvailableTimeSlot = await _availabilityService.bookingWithinAvailabilitySlot(booking.appointmentDate, booking.duration);

            if (!withinAvailableTimeSlot)
            {
                return new BookingResult("TAKEN");
            }

            try
            {
                var created = await _bookingRepository.CreateAsync(booking);

                // Null check
                if (created == null)
                {
                    return new BookingResult("SERVER_ERROR");
                }

                var ukZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

                created.appointmentDate = TimeZoneInfo.ConvertTimeFromUtc(created.appointmentDate, ukZone);

                // await _emailService.Send(created);

                return new BookingResult(created);
            }
            catch (Exception)
            {
                return new BookingResult("SERVER_ERROR");
            }


        
        }
        public async Task<booking?> getById(string id)
        {
            return await _bookingRepository.GetByIdAsync(id);
        }

        public async Task<bool> update(booking updatedBooking)
        {
            if (updatedBooking.Id == null)
            {
                return false;
            }

            var result = await _bookingRepository.UpdateAsync(updatedBooking);
            return result;
        }

        public async Task<bool> markReminderSent(booking updatedBooking)
        {
            var result = await _bookingRepository.MarkReminderSentAsync(updatedBooking);
            return result;
        }

        public async Task<BookingResult> delete(string id)
        {

            var booking = await _bookingRepository.GetByIdAsync(id);

            if (booking == null)
            {
                return new BookingResult("NOT_FOUND");
            }

            var succeeded = await _bookingRepository.DeleteAsync(id);

            if (!succeeded)
            {
                return new BookingResult("SERVER_ERROR");
            }

            await _emailService.sendCancellation(booking);
            return new BookingResult(booking);
        }


        // public async Task<List<TimeSpan>> getAvailableTimes(DateTime date)
        // {
        //     DateTime start = date.Date;
        //     DateTime end = start.AddDays(1);

        //     var bookingsOnDate = await bookingsTable.Find(b => b.appointmentDate >= start && b.appointmentDate < end && (b.bookingStatus == booking.status.Completed || b.bookingStatus == booking.status.Confirmed)).ToListAsync();

        //     List<TimeSpan> bookingTimes = new List<TimeSpan>();

        //     TimeSpan firstSlot = new TimeSpan(9, 0, 0);
        //     TimeSpan lastSlot = new TimeSpan(14, 0, 0);

        //     var ukZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

        //     int slotGap = 1;

        //     var breaks = new List<TimeSpan>();
        //     breaks.Add(new TimeSpan(10, 0, 0));

        //     for (var currentSlot = firstSlot; currentSlot <= lastSlot; currentSlot += TimeSpan.FromHours(slotGap))
        //     {
        //         var localSlot = start + currentSlot;
        //         var utcSlot = TimeZoneInfo.ConvertTimeToUtc(localSlot, ukZone);

        //         bool taken = bookingsOnDate.Any(b =>
        //         {
        //             return utcSlot == b.appointmentDate;
        //         });

        //         bool breakExist = false;

        //         foreach (TimeSpan time in breaks)
        //         {
        //             if (currentSlot == time)
        //             {
        //                 breakExist = true;
        //                 currentSlot += TimeSpan.FromHours(-0.5);
        //             }
        //         }
        //         if (!taken && !breakExist)
        //         {
        //             bookingTimes.Add(currentSlot);
        //         }

        //     }

        //     return bookingTimes;

        // }

    

        public async Task<IEnumerable<booking>> getTodaysBooking(DateTime date)
        {
            var start = date.Date;
            var end = start.AddDays(1);

            var bookings = await _bookingRepository.GetTodaysBookingAsync(start, end); 

            var ukZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

            foreach (booking b in bookings)
            {
                b.appointmentDate = TimeZoneInfo.ConvertTimeFromUtc(b.appointmentDate, ukZone);
            }

            return bookings;

        }

        public async Task<IEnumerable<booking>> getUpcomingBookings(DateTime date)
        {
            var start = date.Date;
            

            var bookings = await _bookingRepository.GetUpcomingBookingsAsync(start);
            var ukZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

            foreach (booking b in bookings)
            {
                b.appointmentDate = TimeZoneInfo.ConvertTimeFromUtc(b.appointmentDate, ukZone);
            }

            return bookings;

        }

        public async Task<IEnumerable<booking>> getNextDayBookings(DateTime date)
        {
            var start = date.Date.AddDays(1);
            var end = start.AddDays(1);

            var bookings = await _bookingRepository.GetNextDayBookingsAsync(start,end);
            return bookings;

        }


        public async Task<int> getWeeklyRevenue(DateTime date)
        {
            var day = ((int)date.DayOfWeek + 6) % 7;

            var currentMonday = date.Date.AddDays(-day);
            var previousMonday = currentMonday.AddDays(-7);
            

            var bookings = await _bookingRepository.getBookingsByDateRange(previousMonday, currentMonday,booking.status.Confirmed);
            int revenue = bookings.Sum(b => b.cost);


            return revenue;

        }

        

        public async Task<int> getMonthlyRevenue(DateTime date)
        {
            var previousMonth = date.AddMonths(-1).Month;
            var currentYear = date.AddMonths(-1).Year;

            var start = new DateTime(currentYear, previousMonth, 1);
            var end = start.AddMonths(1);


            var bookings = await _bookingRepository.getBookingsByDateRange(start, end, booking.status.Confirmed);

            int revenue = bookings.Sum(b => b.cost);

            
            return revenue;

        }



        public async Task<BookingResult> isBookingExpired(string bookingId)
        {



            var booking = await _bookingRepository.GetByIdAsync(bookingId);

            if (booking == null)
            {
                return new BookingResult("NOT_FOUND");
            }

            if (booking.bookingStatus != booking.status.DepositPending)
            {
                return new BookingResult("NOT_EXPIRED");
            }

            if (DateTime.UtcNow > booking.expiryDate.AddSeconds(-25) && booking.bookingStatus == booking.status.DepositPending)
            {
                booking.bookingStatus = booking.status.Expired;
                await _bookingRepository.UpdateAsync(booking);
                
                return new BookingResult("EXPIRED");
            }

            return new BookingResult("NOT_EXPIRED");
        }


        public async Task MarkExpiredBookingsAsync()
        {
            var expiredBookings = await _bookingRepository.GetExpiredBookingsAsync(DateTime.UtcNow);

            foreach (var booking in expiredBookings)
            {
                booking.bookingStatus = booking.status.Expired;
                await _bookingRepository.UpdateAsync(booking);
            }
        }

    }
}