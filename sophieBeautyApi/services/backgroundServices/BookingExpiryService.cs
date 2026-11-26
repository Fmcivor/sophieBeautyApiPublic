using Microsoft.Extensions.Logging;
using sophieBeautyApi.ServiceInterfaces;

namespace sophieBeautyApi.services.backgroundServices
{


    public class BookingExpiryService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BookingExpiryService> _logger;

        public BookingExpiryService(IServiceScopeFactory scopeFactory, ILogger<BookingExpiryService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var bookingService = scope.ServiceProvider.GetRequiredService<IBookingService>();

                    await bookingService.MarkExpiredBookingsAsync();


                    await bookingService.deleteOldExpiredBookingsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in BookingExpiryService");
                }


                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken); // Check every minute
            }

        }
    }

}