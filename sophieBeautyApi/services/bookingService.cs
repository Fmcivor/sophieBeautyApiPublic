using Microsoft.AspNetCore.Http.HttpResults;
using MongoDB.Driver;
using sophieBeautyApi.Models;

namespace sophieBeautyApi.services
{
    public class bookingService
    {
        private readonly IMongoCollection<booking> bookingsTable;
        private readonly MongoClient _mongoClient;

        public bookingService(MongoClient mongoClient)
        {
            _mongoClient = mongoClient;
            var database = _mongoClient.GetDatabase("SophieBeauty");
            bookingsTable = database.GetCollection<booking>("bookings");
        }

        public async Task<IEnumerable<booking>> getAll()
        {
            var bookings = await bookingsTable.Find(b => true).ToListAsync();

            return bookings;
        }

        public async Task<booking> create(booking newBooking)
        {
            await bookingsTable.InsertOneAsync(newBooking);

            return newBooking;
        }


        public async Task<booking?> getById(string id)
        {
            var filter = Builders<booking>.Filter.Eq(b => b.Id, id);
            return await bookingsTable.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<bool> update(booking updatedBooking)
        {
            var filter = Builders<booking>.Filter.Eq(b => b.Id, updatedBooking.Id);
            var result = await bookingsTable.ReplaceOneAsync(filter, updatedBooking);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> markReminderSent(booking updatedBooking)
        {
            var filter = Builders<booking>.Filter.Eq(b => b.Id, updatedBooking.Id);
            var update = Builders<booking>.Update.Set(b => b.reminderSent, true);
            var result = await bookingsTable.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> delete(string id)
        {
            var filter = Builders<booking>.Filter.Eq(b => b.Id, id);
            var result = await bookingsTable.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }

        public async Task<IEnumerable<booking>> bookingsByDate(DateTime date)
        {
            var start = date.Date;
            var end = start.AddDays(1);

            var filter = Builders<booking>.Filter.Gte(b => b.appointmentDate, start) &
                         Builders<booking>.Filter.Lt(b => b.appointmentDate, end);

            var bookings = await bookingsTable.Find(filter).ToListAsync();
            return bookings;
        }

        public async Task<booking> bookingOnDate(DateTime date)
        {
            var booking = await bookingsTable.Find(b => b.appointmentDate == date).FirstOrDefaultAsync();

            return booking;
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

            var bookings = await bookingsTable.Find(b => b.appointmentDate >= start && b.appointmentDate < end && b.bookingStatus == booking.status.Confirmed).ToListAsync();
            return bookings;

        }

        public async Task<IEnumerable<booking>> getUpcomingBookings(DateTime date)
        {
            var start = date.Date;
            

            var bookings = await bookingsTable.Find(b => b.appointmentDate >= start && b.bookingStatus == booking.status.Confirmed).ToListAsync();
            return bookings;

        }

        public async Task<IEnumerable<booking>> getNextDayBookings(DateTime date)
        {
            var start = date.Date.AddDays(1);
            var end = start.AddDays(1);

            var bookings = await bookingsTable.Find(b => b.appointmentDate >= start && b.appointmentDate < end && b.bookingStatus == booking.status.Confirmed).ToListAsync();
            return bookings;

        }


        public async Task<int> getWeeklyRevenue(DateTime date)
        {
            var day = ((int)date.DayOfWeek + 6) % 7;

            var currentMonday = date.Date.AddDays(-day);
            var previousMonday = currentMonday.AddDays(-7);
            var end = currentMonday;

            var bookings = await bookingsTable.Find(b => b.appointmentDate >= previousMonday && b.appointmentDate < currentMonday && b.bookingStatus == booking.status.Completed).ToListAsync();

            int revenue = 0;

            foreach (booking b in bookings)
            {
                revenue += b.cost;
            }

            return revenue;

        }

        public async Task<int> getMonthlyRevenue(DateTime date)
        {
            var previousMonth = date.AddMonths(-1).Month;
            var currentYear = date.AddMonths(-1).Year;

            var start = new DateTime(currentYear, previousMonth, 1);
            var end = start.AddMonths(1);


            var bookings = await bookingsTable.Find(b => b.appointmentDate >= start && b.appointmentDate < end && b.bookingStatus == booking.status.Confirmed).ToListAsync();

            int revenue = 0;

            foreach (booking b in bookings)
            {
                revenue += b.cost;
            }

            return revenue;

        }



    }
}