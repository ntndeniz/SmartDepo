using AkilliDepo.Api.DTOs;
using AkilliDepo.Api.Entities;
using AkilliDepo.Api.Repositories;

namespace AkilliDepo.Api.Managers;

public interface ICompanySettingsManager
{
    Task<ServiceResult<CompanySettingsDto>> GetAsync(string? companyId);
    Task<ServiceResult<CompanySettingsDto>> UpdateAsync(UpdateCompanySettingsRequest request);
}

public class CompanySettingsManager : ICompanySettingsManager
{
    private readonly ICompanySettingsRepository _repository;

    public CompanySettingsManager(ICompanySettingsRepository repository)
    {
        _repository = repository;
    }

    private static CompanySettingsDto ToDto(CompanySettings? s) => s is null
        ? new CompanySettingsDto { IsConfigured = false }
        : new CompanySettingsDto
        {
            CorridorCount = s.CorridorCount,
            ZonesPerCorridor = s.ZonesPerCorridor,
            ShelvesPerZone = s.ShelvesPerZone,
            IsConfigured = true,
            UpdatedAt = s.UpdatedAt
        };

    public async Task<ServiceResult<CompanySettingsDto>> GetAsync(string? companyId)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return ServiceResult<CompanySettingsDto>.BadRequest("CompanyId zorunludur.");

        var settings = await _repository.GetByCompanyIdAsync(companyId);
        return ServiceResult<CompanySettingsDto>.Ok(ToDto(settings));
    }

    public async Task<ServiceResult<CompanySettingsDto>> UpdateAsync(UpdateCompanySettingsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<CompanySettingsDto>.BadRequest("CompanyId zorunludur.");
        if (request.CorridorCount <= 0 || request.ZonesPerCorridor <= 0 || request.ShelvesPerZone <= 0)
            return ServiceResult<CompanySettingsDto>.BadRequest("Koridor, bölge ve raf sayıları sıfırdan büyük olmalıdır.");

        var settings = await _repository.GetByCompanyIdAsync(request.CompanyId);
        if (settings is null)
        {
            settings = new CompanySettings
            {
                CompanyId = request.CompanyId,
                CorridorCount = request.CorridorCount,
                ZonesPerCorridor = request.ZonesPerCorridor,
                ShelvesPerZone = request.ShelvesPerZone,
                UpdatedAt = DateTime.UtcNow
            };
            await _repository.AddAsync(settings);
        }
        else
        {
            settings.CorridorCount = request.CorridorCount;
            settings.ZonesPerCorridor = request.ZonesPerCorridor;
            settings.ShelvesPerZone = request.ShelvesPerZone;
            settings.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(settings);
        }

        await _repository.SaveChangesAsync();

        return ServiceResult<CompanySettingsDto>.Ok(ToDto(settings), "Firma ayarları kaydedildi.");
    }
}
