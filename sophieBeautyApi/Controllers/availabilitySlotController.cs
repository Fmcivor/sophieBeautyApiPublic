using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using sophieBeautyApi.Models;
using sophieBeautyApi.ServiceInterfaces;
using sophieBeautyApi.services;

namespace sophieBeautyApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class availablilitySlotController : ControllerBase
    {
        private readonly IAvailabilitySlotService _availabilitySlotService;

        public availablilitySlotController(IAvailabilitySlotService availablilitySlotService)
        {
            _availabilitySlotService = availablilitySlotService;
        }


        [HttpGet]
        public async Task<ActionResult<IEnumerable<availablilitySlot>>> GetAvailabilitySlots()
        {
            var slots = await _availabilitySlotService.getAll();

            if (slots == null)
            {
                // This indicates a problem with the service/db call
                return StatusCode(500, new { message = "Failed to load availability slots" });
            }

            if (!slots.Any())
            {
                // No availability, but still a valid response
                return Ok(new List<availablilitySlot>());
            }

            return Ok(slots);
        }


        [Authorize]
        [HttpPost("create")]
        public async Task<ActionResult<availablilitySlot>> create([FromBody] availablilitySlot slot)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var newSlot = await _availabilitySlotService.create(slot);

            if (newSlot == null)
            {
                return BadRequest("Overlapping availability slot");
            }

            return CreatedAtAction(nameof(create), newSlot);

        }

        [Authorize]
        [HttpDelete]
        public async Task<ActionResult<bool>> deleteAvailabilitySlot([FromBody] availablilitySlot slot)
        {
            try
            {
                var deleted = await _availabilitySlotService.delete(slot);

                if (!deleted)
                {
                    return NotFound(new { message = "Availability not found" });
                }

                return NoContent(); // 204 when delete succeeds
            }
            catch (FormatException)
            {
                // For example, if "id" is not a valid ObjectId for MongoDB
                return BadRequest(new { message = "Invalid ID format" });
            }
            catch (Exception ex)
            {
                // Log the error
                return StatusCode(500, new { message = "An unexpected error occurred", error = ex.Message });
            }
        }


        // [HttpPost("oldAvailableTimes")]
        // public async Task<ActionResult<IEnumerable<TimeSpan>>> getAvailableTimes([FromBody] DateTime date)
        // {

        //     var slots = await _availabilitySlotService.getSlotsByDate(date);

        //     var allTimes = new List<TimeSpan>();

        //     var bookingsOnDate = await _bookingService.bookingsByDate(date);

        //     var localZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");




        //     foreach (availablilitySlot slot in slots)
        //     {
        //         for (TimeSpan i = slot.startTime; i <= slot.endTime; i = i.Add(TimeSpan.FromHours(1)))
        //         {
        //             bool slotTaken = false;
        //             int bookingDuration = 0;
        //             if (bookingsOnDate.Any(b => TimeZoneInfo.ConvertTimeFromUtc(b.appointmentDate, localZone).TimeOfDay == i))
        //             {
        //                 slotTaken = true;
        //                 bookingDuration = bookingsOnDate.FirstOrDefault(b => TimeZoneInfo.ConvertTimeFromUtc(b.appointmentDate, localZone).TimeOfDay == i).duration;
        //                 if (bookingDuration > 60)
        //                 {
        //                     i += TimeSpan.FromMinutes(bookingDuration - 60);
        //                 }
        //             }



        //             if (!slotTaken)
        //             {
        //                 allTimes.Add(i);
        //             }
        //         }
        //     }

        //     return Ok(allTimes);
        // }



        [Authorize]
        [HttpDelete("deleteAll")]
        public async Task<ActionResult> deleteAll()
        {
            bool success = await _availabilitySlotService.deleteAll();

            if (!success)
            {
                return StatusCode(500, "Failed to delete all slots");
            }

            return NoContent();
        }



        [HttpPost("availableTimes")]
        public async Task<ActionResult<IEnumerable<TimeSpan>>> testGetTimes([FromBody] availableTimesRequest request)
        {
            var times = await _availabilitySlotService.getAvailableTimes(request);
            return Ok(times);
        } 
        






    }
}