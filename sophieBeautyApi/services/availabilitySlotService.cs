using sophieBeautyApi.Models;
using sophieBeautyApi.RepositoryInterfaces;
using sophieBeautyApi.ServiceInterfaces;

namespace sophieBeautyApi.services
{
    public class availablilitySlotService: IAvailabilitySlotService
    {

        private readonly IAvailabilitySlotRepository _availabilitySlotRepository;
        private readonly IBookingRepository _bookingRepository;

        public availablilitySlotService(IAvailabilitySlotRepository availabilitySlotRepository, IBookingRepository bookingRepository)
        {
            this._availabilitySlotRepository = availabilitySlotRepository;
            this._bookingRepository = bookingRepository;
        }


        public async Task<IEnumerable<availablilitySlot>> getAll()
        {
            var availabilitySlots = await _availabilitySlotRepository.GetAllAsync();

            return availabilitySlots;
        }


        public async Task<availablilitySlot?> create(availablilitySlot newSlot)
        {
            var existingSlots = await getSlotsByDate(DateTime.ParseExact(newSlot.date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture));

            // Check for overlapping slots
            if (existingSlots.Any())
            {
                foreach (var slot in existingSlots)
                {
                    if (!(newSlot.endTime < slot.startTime || newSlot.startTime > slot.endTime))
                    {
                        return null;  // Overlap detected
                    }
                }
            }

            return await _availabilitySlotRepository.CreateAsync(newSlot);
        }

        public async Task<bool> bookingWithinAvailabilitySlot(DateTime apptTime, int duration)
        {
            var ukZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

            var bookingUkFullDate = TimeZoneInfo.ConvertTimeFromUtc(apptTime, ukZone);

            var slotsOnDate = await getSlotsByDate(bookingUkFullDate);

            if (slotsOnDate == null)
            {
                return false;
            }


            var bookingTime = bookingUkFullDate.TimeOfDay;
            var bookingEnd = bookingTime.Add(TimeSpan.FromMinutes(duration));

            bool withinSlot = false;

            foreach (availablilitySlot slot in slotsOnDate)
            {
                if (bookingTime >= slot.startTime && bookingEnd <= slot.endTime)
                {
                    withinSlot = true;
                    break;
                }
            }

            if (withinSlot == false)
            {
                return false;
            }

            var start = apptTime.Date;
            var end = start.AddDays(1);

            var existingBookings = await _bookingRepository.GetBookingsByDateAsync(start, end);
            var localZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

            bool overlap = false;

            overlap = existingBookings.Any(b =>
            {
                TimeSpan existingStart = TimeZoneInfo.ConvertTimeFromUtc(b.appointmentDate, localZone).TimeOfDay;
                TimeSpan existingEnd = existingStart.Add(TimeSpan.FromMinutes(b.duration));
                return bookingTime < existingEnd && bookingEnd > existingStart && b.bookingStatus != booking.status.Expired;
            });

            if (overlap)
            {
                return false;
            }

            return true;
        }

        public async Task<bool> delete(availablilitySlot slot)
        {
            return await _availabilitySlotRepository.DeleteAsync(slot);
        }


        public async Task<IEnumerable<TimeSpan>> getAvailableTimes(availableTimesRequest request)
        {

            var slots = await getSlotsByDate(request.date);

            var allTimes = new List<TimeSpan>();


            var start = request.date.Date;
            var end = start.AddDays(1);

            var bookingsOnDate = await _bookingRepository.GetBookingsByDateAsync(start, end);

            var localZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

            // check all availability slots
            foreach (availablilitySlot slot in slots)
            {
                for (TimeSpan i = slot.startTime; i <= slot.endTime; i = i.Add(TimeSpan.FromHours(0.5)))
                {
                    bool slotTaken = bookingsOnDate.Any(b =>
                    {
                        TimeSpan slotStart = i;
                        TimeSpan slotEnd = i.Add(TimeSpan.FromMinutes(request.bookingDuration));
                        TimeSpan existingStart = TimeZoneInfo.ConvertTimeFromUtc(b.appointmentDate, localZone).TimeOfDay;
                        TimeSpan existingEnd = existingStart.Add(TimeSpan.FromMinutes(b.duration));

                        if (slotStart < existingEnd && slotEnd > existingStart)
                        {
                            return true;
                        }
                        return false;
                    });

                    if (!slotTaken)
                    {
                        allTimes.Add(i);
                    }
                }
            }

            return allTimes;

        }



        public async Task<IEnumerable<availablilitySlot>> getSlotsByDate(DateTime date)
        {
            return await _availabilitySlotRepository.GetSlotsByDateAsync(date);
        }



        //dev only
        public async Task<bool> deleteAll()
        {
            return await _availabilitySlotRepository.DeleteAllAsync();
        }

    }
}