
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using sophieBeautyApi.ServiceInterfaces;

namespace sophieBeautyApi.services.startupServices
{
    public class StartupTask : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<StartupTask> _logger;

        public StartupTask(
            IServiceScopeFactory scopeFactory,
            ILogger<StartupTask> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogWarning("STARTUP TASK ENTERED");

            try
            {
                using var scope = _scopeFactory.CreateScope();

                var bookingService = scope.ServiceProvider
                    .GetRequiredService<IBookingService>();

                var availabilityService = scope.ServiceProvider
                    .GetRequiredService<IAvailabilitySlotService>();

                await bookingService.deleteYearOldBookingsAsync();

                await availabilityService.deleteMonthOldAvailabilitySlotsAsync();

                _logger.LogInformation("Startup task completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Startup task failed");
            }
        }
    }
}
