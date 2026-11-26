using MongoDB.Driver;
using sophieBeautyApi.Models;
using sophieBeautyApi.RepositoryInterfaces;

namespace sophieBeautyApi.Repositorys
{
    public class AvailabilitySlotRepositoryMongo : IAvailabilitySlotRepository
    {
        private readonly IMongoCollection<availablilitySlot> _availabilitySlotTable;
        private readonly MongoClient _mongoClient;

        public AvailabilitySlotRepositoryMongo(MongoClient mongoClient)
        {
            _mongoClient = mongoClient;
            var database = _mongoClient.GetDatabase("SophieBeauty");
            _availabilitySlotTable = database.GetCollection<availablilitySlot>("availabilitySlots");
        }

        public async Task<IEnumerable<availablilitySlot>> GetAllAsync()
        {
            var availabilitySlots = await _availabilitySlotTable.Find(a => true).ToListAsync();
            return availabilitySlots;
        }

        public async Task<availablilitySlot> CreateAsync(availablilitySlot newSlot)
        {
            await _availabilitySlotTable.InsertOneAsync(newSlot);
            return newSlot;
        }

        public async Task<bool> BookingWithinAvailabilitySlotAsync(DateTime apptTime)
        {
            var ukZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

            var bookingUkFullDate = TimeZoneInfo.ConvertTimeFromUtc(apptTime, ukZone);

            var slotsOnDate = await GetSlotsByDateAsync(bookingUkFullDate);

            if (slotsOnDate == null)
            {
                return false;
            }

            var bookingTime = bookingUkFullDate.TimeOfDay;

            foreach (availablilitySlot slot in slotsOnDate)
            {
                if (bookingTime >= slot.startTime && bookingTime <= slot.endTime)
                {
                    return true;
                }
            }

            return false;
        }

        public async Task<bool> DeleteAsync(availablilitySlot slot)
        {
            var filter = Builders<availablilitySlot>.Filter.Eq(a => a.Id, slot.Id);
            var result = await _availabilitySlotTable.DeleteOneAsync(filter);
            return result.DeletedCount == 1;
        }

        public async Task<IEnumerable<availablilitySlot>> GetSlotsByDateAsync(DateTime date)
        {
            var slots = await _availabilitySlotTable.Find(a => a.date == date.ToString("yyyy-MM-dd")).ToListAsync();
            return slots;
        }

        public async Task<bool> DeleteAllAsync()
        {
            await _availabilitySlotTable.DeleteManyAsync(a => true);
            return true;
        }
    }
}
