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

        public paymentController(IBookingRepository bookingRepository)
        {
            this._bookingRepository = bookingRepository;
        }


        [HttpPost("create-payment-intent")]
        public async Task<ActionResult> createPaymentIntent([FromBody] booking booking)
        {
            try{
            var metadata = new Dictionary<string, string>
            {
                {"bookingId", booking.Id.ToString()},
                {"customerName", booking.customerName},

            };


            int depositDue = (int) Math.Round(booking.cost * 0.25) ;

            var option = new PaymentIntentCreateOptions
            {
                Amount = depositDue*100,
                Currency = "gbp",
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true,
                },
                Metadata = metadata,
                ReceiptEmail = booking.email
            };

            var service = new PaymentIntentService();
            PaymentIntent intent = await service.CreateAsync(option);

            booking.stripePaymentId = intent.Id;

            // update the intent id 
            await _bookingRepository.UpdateAsync(booking);


            return Ok(new { clientSecret = intent.ClientSecret, reservedBooking = booking });
            }
            catch (StripeException stripeEx)
            {

                if (booking != null)
                {
                    // await _bookingService.cancelBooking(booking._id);
                }

                return StatusCode(StatusCodes.Status500InternalServerError, new {message = "Error creating payment intent", details = stripeEx.Message});

            }
            catch(Exception ex)
            {
                if (booking != null)
                {
                    // await _bookingService.cancelBooking(booking._id);
                }

                return StatusCode(StatusCodes.Status500InternalServerError, new { message = "An unexpected error occurred.", details = ex.Message });

            }

            return null;
        }

    }
}

