using AkilliDepo.Api.DTOs;
using AkilliDepo.Api.Entities;
using AkilliDepo.Api.Repositories;

namespace AkilliDepo.Api.Managers;

public interface ILocationManager
{
    Task<ServiceResult<PagedResponse<LocationDto>>> GetPagedAsync(LocationPagedRequest request);
    Task<ServiceResult<LocationDto>> GetByIdAsync(int id, string? companyId);
    Task<ServiceResult<GenerateLocationsResultDto>> GenerateAsync(GenerateLocationsRequest request);
    Task<ServiceResult<bool>> DeleteAsync(DeleteRequest request);
    Task<ServiceResult<LocationDto>> AssignBoxAsync(AssignBoxRequest request);
    Task<ServiceResult<LocationDto>> ReleaseAsync(ReleaseLocationRequest request);
}

public class LocationManager : ILocationManager
{
    private readonly ILocationRepository _repository;
    private readonly IBoxRepository _boxRepository;
    private readonly ICompanySettingsRepository _settingsRepository;

    public LocationManager(ILocationRepository repository, IBoxRepository boxRepository, ICompanySettingsRepository settingsRepository)
    {
        _repository = repository;
        _boxRepository = boxRepository;
        _settingsRepository = settingsRepository;
    }

    private static LocationDto ToDto(Location l) => new()
    {
        Id = l.Id,
        CompanyId = l.CompanyId,
        CorridorNo = l.CorridorNo,
        ZoneNo = l.ZoneNo,
        ShelfNo = l.ShelfNo,
        Barcode = l.Barcode,
        IsOccupied = l.IsOccupied,
        CurrentBoxId = l.CurrentBoxId,
        CurrentBoxBarcode = l.CurrentBox?.Barcode,
        CurrentBoxProductName = l.CurrentBox?.Product?.Name
    };

    public async Task<ServiceResult<PagedResponse<LocationDto>>> GetPagedAsync(LocationPagedRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<PagedResponse<LocationDto>>.BadRequest("CompanyId zorunludur.");

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 25 : request.PageSize;

        var (items, totalCount) = await _repository.GetPagedAsync(
            request.CompanyId, page, pageSize, request.Search, request.IsOccupied);

        return ServiceResult<PagedResponse<LocationDto>>.Ok(new PagedResponse<LocationDto>
        {
            Data = items.Select(ToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    public async Task<ServiceResult<LocationDto>> GetByIdAsync(int id, string? companyId)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return ServiceResult<LocationDto>.BadRequest("CompanyId zorunludur.");

        var location = await _repository.GetByIdAsync(id);
        if (location is null)
            return ServiceResult<LocationDto>.NotFound("Konum bulunamadı.");
        if (location.CompanyId != companyId)
            return ServiceResult<LocationDto>.Forbidden("Bu konuma erişim yetkiniz yok.");

        return ServiceResult<LocationDto>.Ok(ToDto(location));
    }

    public async Task<ServiceResult<GenerateLocationsResultDto>> GenerateAsync(GenerateLocationsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<GenerateLocationsResultDto>.BadRequest("CompanyId zorunludur.");

        var settings = await _settingsRepository.GetByCompanyIdAsync(request.CompanyId);
        if (settings is null)
            return ServiceResult<GenerateLocationsResultDto>.BadRequest(
                "Depo boyutları henüz ayarlanmamış. Önce Ayarlar ekranından koridor/bölge/raf sayılarını girin.");

        var existing = await _repository.GetExistingCoordinatesAsync(request.CompanyId);
        var existingSet = existing.ToHashSet();

        var createdCount = 0;
        for (var c = 1; c <= settings.CorridorCount; c++)
        {
            for (var z = 1; z <= settings.ZonesPerCorridor; z++)
            {
                for (var s = 1; s <= settings.ShelvesPerZone; s++)
                {
                    if (existingSet.Contains((c, z, s)))
                        continue;

                    await _repository.AddAsync(new Location
                    {
                        CompanyId = request.CompanyId,
                        CorridorNo = c,
                        ZoneNo = z,
                        ShelfNo = s,
                        Barcode = $"K{c}-B{z}-R{s}",
                        IsOccupied = false
                    });
                    createdCount++;
                }
            }
        }

        await _repository.SaveChangesAsync();

        var totalCount = existingSet.Count + createdCount;
        return ServiceResult<GenerateLocationsResultDto>.Ok(
            new GenerateLocationsResultDto { CreatedCount = createdCount, TotalCount = totalCount },
            createdCount > 0 ? $"{createdCount} yeni konum oluşturuldu." : "Yeni konum oluşturulmadı, hepsi zaten mevcuttu.");
    }

    public async Task<ServiceResult<bool>> DeleteAsync(DeleteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<bool>.BadRequest("CompanyId zorunludur.");

        var location = await _repository.GetByIdAsync(request.Id);
        if (location is null)
            return ServiceResult<bool>.NotFound("Konum bulunamadı.");
        if (location.CompanyId != request.CompanyId)
            return ServiceResult<bool>.Forbidden("Bu konuma erişim yetkiniz yok.");
        if (location.IsOccupied)
            return ServiceResult<bool>.BadRequest("Dolu konum silinemez. Önce koliyi kaldırın.");

        location.IsDeleted = true;
        await _repository.UpdateAsync(location);
        await _repository.SaveChangesAsync();

        return ServiceResult<bool>.Ok(true, "Konum silindi.");
    }

    public async Task<ServiceResult<LocationDto>> AssignBoxAsync(AssignBoxRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<LocationDto>.BadRequest("CompanyId zorunludur.");
        if (string.IsNullOrWhiteSpace(request.BoxBarcode))
            return ServiceResult<LocationDto>.BadRequest("Koli barkodu zorunludur.");

        var location = await _repository.GetByIdAsync(request.LocationId);
        if (location is null)
            return ServiceResult<LocationDto>.NotFound("Konum bulunamadı.");
        if (location.CompanyId != request.CompanyId)
            return ServiceResult<LocationDto>.Forbidden("Bu konuma erişim yetkiniz yok.");
        if (location.IsOccupied)
            return ServiceResult<LocationDto>.BadRequest("Bu konum dolu. Boş bir konum seçin.");

        var box = await _boxRepository.GetByBarcodeAsync(request.CompanyId, request.BoxBarcode);
        if (box is null)
            return ServiceResult<LocationDto>.NotFound("Bu barkoda sahip koli bulunamadı.");
        if (box.Status != BoxStatus.InStock)
            return ServiceResult<LocationDto>.BadRequest("Yalnızca stoktaki koliler rafa yerleştirilebilir.");

        location.IsOccupied = true;
        location.CurrentBoxId = box.Id;
        await _repository.UpdateAsync(location);

        box.Status = BoxStatus.OnShelf;
        await _boxRepository.UpdateAsync(box);

        await _repository.SaveChangesAsync();

        var updated = await _repository.GetByIdAsync(location.Id);
        return ServiceResult<LocationDto>.Ok(ToDto(updated!), "Koli rafa yerleştirildi.");
    }

    public async Task<ServiceResult<LocationDto>> ReleaseAsync(ReleaseLocationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<LocationDto>.BadRequest("CompanyId zorunludur.");

        var location = await _repository.GetByIdAsync(request.LocationId);
        if (location is null)
            return ServiceResult<LocationDto>.NotFound("Konum bulunamadı.");
        if (location.CompanyId != request.CompanyId)
            return ServiceResult<LocationDto>.Forbidden("Bu konuma erişim yetkiniz yok.");

        if (location.CurrentBoxId.HasValue)
        {
            var box = await _boxRepository.GetByIdAsync(location.CurrentBoxId.Value);
            if (box is not null && box.Status == BoxStatus.OnShelf)
            {
                box.Status = BoxStatus.InStock;
                await _boxRepository.UpdateAsync(box);
            }
        }

        location.IsOccupied = false;
        location.CurrentBoxId = null;
        await _repository.UpdateAsync(location);
        await _repository.SaveChangesAsync();

        return ServiceResult<LocationDto>.Ok(ToDto(location), "Konum boşaltıldı.");
    }
}
