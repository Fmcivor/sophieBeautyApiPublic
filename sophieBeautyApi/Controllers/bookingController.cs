using System.IO.Pipelines;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Driver.Search;
using sophieBeautyApi.Models;
using sophieBeautyApi.services;

namespace sophieBeautyApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class bookingController : ControllerBase
    {
        private readonly bookingService _bookingService;
        private readonly treatmentService _treatmentService;

        private readonly emailService _emailService;

        private readonly availablilitySlotService _availablilitySlotService;

        public bookingController(bookingService bookingService, treatmentService treatmentService, availablilitySlotService availablilitySlotService, emailService emailService)
        {
            this._bookingService = bookingService;
            this._treatmentService = treatmentService;
            this._availablilitySlotService = availablilitySlotService;
            this._emailService = emailService;
        }


        [Authorize]
        [HttpGet("Allbookings")]
        public async Task<ActionResult> getAll()
        {
            var bookings = await _bookingService.getAll();

            var ukZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");



            foreach (var b in bookings)
            {
                b.appointmentDate = TimeZoneInfo.ConvertTimeFromUtc(b.appointmentDate, ukZone);
            }



            return Ok(bookings);
        }

        [HttpPost("Create")]
        public async Task<ActionResult> create([FromBody] newBookingDTO newBooking)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

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

            booking booking = new booking(newBooking.customerName, newBooking.appointmentDate, newBooking.email, treatmentNames, price,duration, newBooking.payByCard, paid, booking.status.Confirmed, newBooking.phoneNumber);

            var existingBooking = await _bookingService.bookingOnDate(booking.appointmentDate);

            if (existingBooking != null)
            {
                return BadRequest("TAKEN,Sorry the booking slot has already been taken");
            }

            bool withinAvailableTimeSlot = await _availablilitySlotService.bookingWithinAvailabilitySlot(booking.appointmentDate);

            if (!withinAvailableTimeSlot)
            {
                return BadRequest("There is no availability slot for the time chosen");
            }

            booking = await _bookingService.create(booking);

            // Null check
            if (booking == null)
            {
                return StatusCode(500, "An error occurred while creating the booking.");
            }

            await _emailService.Send(booking);

            var ukZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

            booking.appointmentDate = TimeZoneInfo.ConvertTimeFromUtc(booking.appointmentDate, ukZone);

            return CreatedAtAction(nameof(create), booking);
        }


        // [Authorize]
        // [HttpPost("specialCreate")]
        // public async Task<ActionResult> specialCreate([FromBody] newBookingDTO newBooking)
        // {
        //     if (!ModelState.IsValid)
        //     {
        //         return BadRequest(ModelState);
        //     }

        //     var treatment = await _treatmentService.getById(newBooking.treatmentId);

        //     bool paid = false;
        //     if (newBooking.payByCard)
        //     {
        //         paid = true;
        //     }

        //     booking booking = new booking(newBooking.customerName, newBooking.appointmentDate, newBooking.email, treatment.name, (int)treatment.price, treatment.duration, newBooking.payByCard, paid, booking.status.Confirmed);

        //     var existingBooking = await _bookingService.bookingOnDate(booking.appointmentDate);

        //     if (existingBooking != null)
        //     {
        //         return BadRequest("TAKEN,Sorry the booking slot has already been taken");
        //     }

        //     booking = await _bookingService.create(booking);

        //     var ukZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

        //     booking.appointmentDate = TimeZoneInfo.ConvertTimeFromUtc(booking.appointmentDate, ukZone);

        //     return CreatedAtAction(nameof(create), booking);
        // }



        [Authorize]
        [HttpPut("Update")]
        public async Task<ActionResult> update([FromBody] booking updatedbooking)
        {

            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (updatedbooking.Id == null)
            {
                return NotFound();
            }

            bool succeeded = await _bookingService.update(updatedbooking);

            if (!succeeded)
            {
                return StatusCode(500, "An internal error occurred while updating the booking.");
            }

            return Ok();
        }

        [Authorize]
        [HttpDelete("Delete/{id}")]
        public async Task<ActionResult> delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return BadRequest("Id is required.");
            }

            var cancelledBooking = await _bookingService.getById(id);
            bool succeeded = await _bookingService.delete(id);

            if (!succeeded)
            {
                return NotFound();
            }

            await _emailService.sendCancellation(cancelledBooking);
            return NoContent();
        }

        // [HttpPost("availableTimes")]
        // public async Task<ActionResult<IEnumerable<TimeSpan>>> getAvailableTimes([FromBody] DateTime selectedDate)
        // {
        //     var availableTimes = await _bookingService.getAvailableTimes(selectedDate);

        //     return Ok(availableTimes);
        // }




        //admin data

        [Authorize]
        [HttpPost("todaysAppts")]
        public async Task<ActionResult<IEnumerable<booking>>> todaysBookings([FromBody] DateTime today)
        {
            var bookingsToday = await _bookingService.getTodaysBooking(today);

            if (bookingsToday == null)
            {
                return NotFound();
            }

            var ukZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

            foreach (booking b in bookingsToday)
            {
                b.appointmentDate = TimeZoneInfo.ConvertTimeFromUtc(b.appointmentDate, ukZone);
            }

            return Ok(bookingsToday);
        }


        [Authorize]
        [HttpPost("upcomingAppts")]
        public async Task<ActionResult<IEnumerable<booking>>> upcomingBookings([FromBody] DateTime today)
        {
            var upcomingBookings = await _bookingService.getUpcomingBookings(today);

            if (upcomingBookings == null)
            {
                return NotFound();
            }

            var ukZone = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

            foreach (booking b in upcomingBookings)
            {
                b.appointmentDate = TimeZoneInfo.ConvertTimeFromUtc(b.appointmentDate, ukZone);
            }

            return Ok(upcomingBookings);

        }

        [Authorize]
        [HttpPost("lastWeeksRevenue")]
        public async Task<ActionResult<int>> weekRevenue([FromBody] DateTime today)
        {
            var revenue = await _bookingService.getWeeklyRevenue(today);

            return Ok(revenue);
        }

        [Authorize]
        [HttpPost("lastMonthsRevenue")]
        public async Task<ActionResult<int>> monthRevenue([FromBody] DateTime today)
        {
            var revenue = await _bookingService.getMonthlyRevenue(today);

            return Ok(revenue);
        }



    }
}