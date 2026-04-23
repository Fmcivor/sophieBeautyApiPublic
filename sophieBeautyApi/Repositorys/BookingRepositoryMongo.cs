using MongoDB.Driver;
using sophieBeautyApi.Models;
using sophieBeautyApi.RepositoryInterfaces;

namespace sophieBeautyApi.Repositorys
{
    public class BookingRepositoryMongo : IBookingRepository
    {
        private readonly IMongoCollection<booking> bookingsTable;
        private readonly MongoClient _mongoClient;

        public BookingRepositoryMongo(MongoClient mongoClient)
        {
            _mongoClient = mongoClient;
            var database = _mongoClient.GetDatabase("SophieBeauty");
            bookingsTable = database.GetCollection<booking>("bookings");
        }

        public async Task<IEnumerable<booking>> GetAllAsync()
        {
            var bookings = await bookingsTable.Find(b => true).ToListAsync();
            return bookings;
        }

        public async Task<booking> CreateAsync(booking newBooking)
        {
            await bookingsTable.InsertOneAsync(newBooking);
            return newBooking;
        }

        public async Task<booking?> GetByIdAsync(string id)
        {
            var filter = Builders<booking>.Filter.Eq(b => b.Id, id);
            return await bookingsTable.Find(filter).FirstOrDefaultAsync();
        }

        public async Task<bool> UpdateAsync(booking updatedBooking)
        {
            var filter = Builders<booking>.Filter.Eq(b => b.Id, updatedBooking.Id);
            var result = await bookingsTable.ReplaceOneAsync(filter, updatedBooking);
            return result.ModifiedCount>1;
        }

        public async Task<bool> MarkReminderSentAsync(booking updatedBooking)
        {
            var filter = Builders<booking>.Filter.Eq(b => b.Id, updatedBooking.Id);
            var update = Builders<booking>.Update.Set(b => b.reminderSent, true);
            var result = await bookingsTable.UpdateOneAsync(filter, update);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var filter = Builders<booking>.Filter.Eq(b => b.Id, id);
            var result = await bookingsTable.DeleteOneAsync(filter);
            return result.DeletedCount > 0;
        }

        public async Task<IEnumerable<booking>> GetBookingsByDateAsync(DateTime start, DateTime end)
        {
            var filter = Builders<booking>.Filter.Gte(b => b.appointmentDate, start) &
                         Builders<booking>.Filter.Lt(b => b.appointmentDate, end);

            var bookings = await bookingsTable.Find(filter).ToListAsync();
            return bookings;
        }

        public async Task<booking?> GetBookingOnDateAsync(DateTime date)
        {
            var booking = await bookingsTable.Find(b => b.appointmentDate == date).FirstOrDefaultAsync();
            return booking;
        }

        public async Task<IEnumerable<booking>> GetTodaysBookingAsync(DateTime start, DateTime end)
        {
            
            var bookings = await bookingsTable.Find(
                b => b.appointmentDate >= start && 
                b.appointmentDate < end &&
                b.bookingStatus == booking.status.Confirmed
            ).ToListAsync();
            return bookings;
        }

        public async Task<IEnumerable<booking>> GetUpcomingBookingsAsync(DateTime start)
        {

            var bookings = await bookingsTable.Find(b => b.appointmentDate >= start && b.bookingStatus == booking.status.Confirmed).ToListAsync();
            return bookings;
        }

        public async Task<IEnumerable<booking>> GetNextDayBookingsAsync(DateTime start, DateTime end)
        {
            var bookings = await bookingsTable.Find(
                b => b.appointmentDate >= start &&
                b.appointmentDate < end && 
                b.bookingStatus == booking.status.Confirmed
            ).ToListAsync();

            return bookings;
        }

        public async Task<IEnumerable<booking>> getBookingsByDateRange(DateTime start, DateTime end, booking.status status)
        {
            
            var bookings = await bookingsTable.Find(b => b.appointmentDate >= start && b.appointmentDate < end && b.bookingStatus == status).ToListAsync();

            return bookings;
        }

        
    }
}
