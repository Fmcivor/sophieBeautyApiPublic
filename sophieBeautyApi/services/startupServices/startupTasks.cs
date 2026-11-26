using Microsoft.Extensions.Logging;
using sophieBeautyApi.ServiceInterfaces;

namespace sophieBeautyApi.services.startupServices
{

    public class StartupTask : IHostedService
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

    public async Task StartAsync(CancellationToken cancellationToken)
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

            _logger.LogInformation("Startup task completed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Startup task failed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

}