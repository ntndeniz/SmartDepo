using AkilliDepo.Api.Managers;
using AkilliDepo.Api.Repositories;

namespace AkilliDepo.Api.Services;

/// <summary>
/// Uygulama açılışında ve her 6 saatte bir, her şirket için henüz üretilmemiş geçmiş haftaların
/// Mal Kabul / Sevkiyat CSV raporlarını arka planda üretir (bkz. WeeklyReportManager.GenerateMissingWeeksAsync).
/// Kullanıcı hiçbir şey tetiklemeden raporlar birikir; "Raporlar" ekranından her zaman indirilebilir.
/// </summary>
public class WeeklyReportBackgroundService : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(6);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<WeeklyReportBackgroundService> _logger;

    public WeeklyReportBackgroundService(IServiceScopeFactory scopeFactory, ILogger<WeeklyReportBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await GenerateForAllCompaniesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Haftalık rapor üretimi sırasında hata oluştu.");
            }

            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                // Uygulama kapanıyor.
            }
        }
    }

    private async Task GenerateForAllCompaniesAsync()
    {
        using var scope = _scopeFactory.CreateScope();
        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var reportManager = scope.ServiceProvider.GetRequiredService<IWeeklyReportManager>();

        var companyIds = await userRepository.GetDistinctCompanyIdsAsync();
        foreach (var companyId in companyIds)
        {
            await reportManager.GenerateMissingWeeksAsync(companyId, DateTime.UtcNow);
        }
    }
}
