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
                        return BadRequest("TAKEN, Sorry the booking slot has already been taken");
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


            //remove 25 seconds from expiray time

            result.Booking.expiryDate = result.Booking.expiryDate.AddSeconds(-25);

            // return CreatedAtAction(nameof(create), result.Booking.Id);
            return CreatedAtAction(nameof(create), result.Booking);
        }


        [Authorize]
        [HttpPost("Create-Admin")]
        public async Task<ActionResult> createAdmin([FromBody] newBookingDTO newBooking)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }


            var result = await _bookingService.createBookingAdmin(newBooking);

            if (result.IsSuccess == false)
            {
                switch (result.Error)
                {
                    case "TAKEN":
                        return BadRequest("TAKEN, Sorry the booking slot has already been taken");
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


            
            // return CreatedAtAction(nameof(create), result.Booking.Id);
            return CreatedAtAction(nameof(create), result.Booking);
        }


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
                        return NotFound("Booking not found");
                    default:
                        return StatusCode(500, "An unexpected error occurred");
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



        [HttpPost("expired")]
        public async Task<ActionResult<bool>> isExpired([FromBody] String bookingId)
        {


            var result = await _bookingService.isBookingExpired(bookingId);

            switch (result.Error)
            {
                case "NOT_FOUND":
                    return NotFound("Booking not found");
                case "EXPIRED":
                    return Ok(true);
                default:
                    return Ok(false);
            }
        }


        [HttpGet("{bookingId}/depositPaid")]
        public async Task<ActionResult<bool>> depositPaid(string bookingId)
        {
            var booking = await _bookingService.getById(bookingId);

            if (booking == null)
            {
                return NotFound("Booking not found");
            }

            if (booking.bookingStatus == booking.status.Confirmed)
            {
                return Ok(true);
            }
            return Ok(false);


        }


        [HttpGet("{bookingId}/canRetryPayment")]
        public async Task<ActionResult<bool>> canRetryPayment(string bookingId)
        {
            var b = await _bookingService.getById(bookingId);

            if (b == null)
            {
                return NotFound("Booking not found");
            }

            bool canRetry = false;

            if (b.expiryDate < DateTime.UtcNow && b.bookingStatus != booking.status.Confirmed && b.bookingStatus != booking.status.Expired)
            {
                canRetry = true;
            }
            



            return Ok(canRetry);
    }


        [HttpPut("{bookingId}/markExpired")]
        public async Task<ActionResult> markExpired(string bookingId)
        {
            await _bookingService.MarkExpiredBookingsAsync();

            return Ok();
        }

        [HttpGet("{bookingId}/expiryTime")]
        public async Task<ActionResult> getExpiryTime(String bookingId){

            var expiryTime =await _bookingService.getExpiryTime(bookingId);
            if(expiryTime == null){
                return NotFound("Booking not found");
            }

            expiryTime = expiryTime.Value.AddSeconds(-25);
            return Ok(expiryTime);
            
        }

    }
}