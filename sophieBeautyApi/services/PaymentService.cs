
using sophieBeautyApi.Models;
using sophieBeautyApi.RepositoryInterfaces;
using sophieBeautyApi.ServiceInterfaces;
using Stripe;

namespace sophieBeautyApi.services
{

    public class PaymentService : IPaymentService
    {

        private readonly IBookingRepository _bookingRepo;
        public PaymentService(IBookingRepository bookingRepo)
        {
            _bookingRepo = bookingRepo;
        }

        public async Task<string?> CreatePaymentIntent(booking b)
        {
            //null check done in booking controller

            if (string.IsNullOrWhiteSpace(b.Id))
            {
                return null;
            }


            // define payment metadata
            var metadata = new Dictionary<String, String>
            {
                { "bookingId", b.Id.ToString() },
                { "customerName", b.customerName }
            };

            // calculate deposit (converted to pennies)
            int depositDue = (int) Math.Round(b.cost * 0.25);
            depositDue = depositDue *100;

            // define transaction information

            var options = new PaymentIntentCreateOptions
            {
                Amount = depositDue,
                Currency = "gbp",
                AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
                {
                    Enabled = true
                },
                Metadata = metadata,
                ReceiptEmail = b.email
            };

            var service = new PaymentIntentService();
            PaymentIntent? intent = null;

            try
            {
                intent = await service.CreateAsync(options);

                b.stripePaymentId = intent.Id;

                await _bookingRepo.UpdateAsync(b);

                return intent.ClientSecret;
            }
            catch (System.Exception)
            {
                if (intent != null)
                {
                    await service.CancelAsync(intent.Id);
                }
                
                await _bookingRepo.DeleteAsync(b.Id);
                return null;
            }
        }

        
    }
}