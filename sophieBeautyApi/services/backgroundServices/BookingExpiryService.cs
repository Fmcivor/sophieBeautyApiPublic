using sophieBeautyApi.ServiceInterfaces;

namespace sophieBeautyApi.services.backgroundServices
{


    public class BookingExpiryService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public BookingExpiryService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
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
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in BookingExpiryService: {ex.Message}");
                    
                }


                await Task.Delay(TimeSpan.FromMinutes(1), cancellationToken); // Check every minute
            }

        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

}