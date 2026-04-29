using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using MongoDB.Driver;
using sophieBeautyApi.Models;
using sophieBeautyApi.services;
using Newtonsoft.Json;
using Stripe;
using sophieBeautyApi.ServiceInterfaces;
using sophieBeautyApi.RepositoryInterfaces;

namespace sophieBeautyApi.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class paymentController : ControllerBase
    {

        private readonly IBookingRepository _bookingRepository;
        private readonly IEmailService _emailService;

        public paymentController(IBookingRepository bookingRepository, IEmailService emailService)
        {
            this._bookingRepository = bookingRepository;
            this._emailService = emailService;
        }


        [HttpPost("create-payment-intent")]
        public async Task<IActionResult> CreatePaymentIntent(string bookingId)
        {
            TimeZoneInfo timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

            booking? booking = null;

            try
            {
                booking = await _bookingRepository.GetByIdAsync(bookingId);

                if (booking == null)
                {
                    return NotFound(new { message = "Booking not found" });
                }

                var metadata = new Dictionary<string, string>
                {
                    { "bookingId", booking.Id.ToString() },
                    { "customerName", booking.customerName },
                };

                int depositDue = (int)Math.Round(booking.cost * 0.25);

                
                if (depositDue <= 0)
                {
                    try
                    {
                        booking.stripePaymentId = "no payment required";
                        booking.paid = true;
                        booking.bookingStatus = booking.status.Confirmed;

                        await _bookingRepository.UpdateAsync(booking);
                    }
                    catch (Exception)
                    {
                        return StatusCode(500, new { message = "Failed to confirm booking" });
                    }


                    await _emailService.Send(booking);
                    booking.appointmentDate = TimeZoneInfo.ConvertTimeFromUtc(booking.appointmentDate, timeZoneInfo);
                    
                    return Ok(new
                    {
                        clientSecret = (string?)null,
                        reservedBooking = booking
                    });
                }

                
                var options = new PaymentIntentCreateOptions
                {
                    Amount = depositDue * 100,
                    Currency = "gbp",
                    AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                    {
                        Enabled = true,
                    },
                    Metadata = metadata,
                    ReceiptEmail = booking.email
                };

                var service = new PaymentIntentService();
                PaymentIntent intent = await service.CreateAsync(options);

                try
                {
                    booking.stripePaymentId = intent.Id;
                    await _bookingRepository.UpdateAsync(booking);
                }
                catch (Exception)
                {
                    // rollback Stripe side to avoid mismatch
                    await service.CancelAsync(intent.Id);

                    return StatusCode(500, new { message = "Failed to save payment intent" });
                }

                booking.appointmentDate = TimeZoneInfo.ConvertTimeFromUtc(booking.appointmentDate, timeZoneInfo);

                return Ok(new
                {
                    clientSecret = intent.ClientSecret,
                    reservedBooking = booking
                });
            }
            catch (StripeException stripeEx)
            {
                // optional: cancel booking if needed
                if (booking != null)
                {
                    // await _bookingService.cancelBooking(booking.Id);
                }

                return StatusCode(500, new
                {
                    message = "Error creating payment intent",
                    details = stripeEx.Message
                });
            }
            catch (Exception ex)
            {
                if (booking != null)
                {
                    // await _bookingService.cancelBooking(booking.Id);
                }

                return StatusCode(500, new
                {
                    message = "An unexpected error occurred",
                    details = ex.Message
                });
            }
        }

    }
}

