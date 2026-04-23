using System.IO.Pipelines;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MongoDB.Driver.Search;
using sophieBeautyApi.Models;
using sophieBeautyApi.ServiceInterfaces;
using sophieBeautyApi.services;

namespace sophieBeautyApi.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class bookingController : ControllerBase
    {
        private readonly IBookingService _bookingService;
        
        

        

        public bookingController(IBookingService bookingService)
        {
            this._bookingService = bookingService;
            
        }


        [Authorize]
        [HttpGet("Allbookings")]
        public async Task<ActionResult> getAll()
        {
            var bookings = await _bookingService.getAll();

            return Ok(bookings);
        }

        [HttpPost("Create")]
        public async Task<ActionResult> create([FromBody] newBookingDTO newBooking)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }


            var result = await _bookingService.create(newBooking);

            if (result.IsSuccess == false)
            {
                switch (result.Error)
                {
                    case "TAKEN":
                        return BadRequest("TAKEN, Sorry the booking slo has already been taken");
                    case "NO_SLOT":
                        return BadRequest("There is no availability slot for the time chosen");
                    case "SERVER_ERROR":
                        return StatusCode(500, "An error occurred while creating the booking");
                    default:
                        return StatusCode(500, "An unexpected error occurred");
                }
            }

            if (result.Booking == null)
            {
                return StatusCode(500, "An error occurred when creating the booking"); 
            }

            return CreatedAtAction(nameof(create), result.Booking.Id);

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

            var result = await _bookingService.delete(id);

            if (result.IsSuccess == false)
            {
                switch (result.Error)
                {
                    case "NOT_FOUND":
                        return  NotFound("Booking not found");
                    default:
                        return StatusCode(500,"An unexpected error occurred");
                }
            }
            
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

            if (bookingsToday.Any() == false)
            {
                return NotFound();
            }

    
            return Ok(bookingsToday);
        }


        [Authorize]
        [HttpPost("upcomingAppts")]
        public async Task<ActionResult<IEnumerable<booking>>> upcomingBookings([FromBody] DateTime today)
        {
            var upcomingBookings = await _bookingService.getUpcomingBookings(today);

            if (upcomingBookings.Any() == false)
            {
                return NotFound();
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