namespace CellPhoneContactsAPI.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

public class RefreshTokenCleanupService : BackgroundService
{
    private readonly AuthService _authService;
    private readonly ILogger<RefreshTokenCleanupService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public RefreshTokenCleanupService(AuthService authService, ILogger<RefreshTokenCleanupService> logger)
    {
        _authService = authService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Starting refresh token cleanup background service.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _authService.CleanupExpiredRefreshTokens();
                _logger.LogInformation("Expired and revoked refresh tokens cleaned up successfully.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during refresh token cleanup.");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }
}

