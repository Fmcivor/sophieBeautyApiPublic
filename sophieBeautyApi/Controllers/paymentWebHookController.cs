using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using sophieBeautyApi.Models;
using sophieBeautyApi.RepositoryInterfaces;
using sophieBeautyApi.ServiceInterfaces;
using Stripe;

namespace sophieBeautyApi
{
    [Route("api/[controller]")]
    [ApiController]
    public class paymentWebHookController : ControllerBase
    {

        private readonly IBookingService bookingService;

        private readonly IBookingRepository bookingRepository;
        private readonly IEmailService emailService;
        public paymentWebHookController(IBookingService bookingService, IEmailService emailService, IBookingRepository bookingRepository)
        {
            this.bookingService = bookingService;
            this.emailService = emailService;
            this.bookingRepository = bookingRepository;
        }


        [HttpPost]
        public async Task<IActionResult> HandleWebhook()
        {
            var jsonResult = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();

            try
            {
                var stripeEvent = EventUtility.ParseEvent(jsonResult, throwOnApiVersionMismatch: false);

                if (stripeEvent.Type == EventTypes.PaymentIntentSucceeded)
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                    var bookingId = paymentIntent.Metadata["bookingId"];

                    var booking = await bookingRepository.GetByIdAsync(bookingId);

                    await handPaymentSucceeded(booking);

                }
                else if (stripeEvent.Type == EventTypes.PaymentIntentPaymentFailed)
                {
                    var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
                   
                    var bookingId = paymentIntent.Metadata["bookingId"];
                    var booking = await bookingRepository.GetByIdAsync(bookingId);

                    // await handlePaymentFailed(booking);
                    return Ok();
                }
                else
                {
                    Console.WriteLine($"Unhandled event type: {stripeEvent.Type}");
                }

                return Ok();
            }
            catch (StripeException ex)
            {
                Console.WriteLine($"Stripe error: {ex.Message}");
                return BadRequest();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"General error: {ex.Message}");
                return BadRequest();
            }


        }



        private async Task handPaymentSucceeded(booking booking)
        {

            booking.bookingStatus = booking.status.Confirmed;

            await emailService.Send(booking);

            if (!await bookingRepository.UpdateAsync(booking))
            {
                Console.WriteLine("Failed to update booking.");
                return;
            }

        }

        // private async Task handlePaymentFailed(booking booking)
        // {
        //     booking.bookingStatus = booking.status.Expired;


        //     // await emailService.SendPaymentFailedEmail(booking);

        //     await bookingRepository.UpdateAsync(booking);

        //     if (booking.Id != null)
        //     {
        //         await bookingRepository.DeleteAsync(booking.Id);
        //     }
        // }
    }
}
