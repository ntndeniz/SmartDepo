using AkilliDepo.Api.DTOs;
using AkilliDepo.Api.Entities;
using AkilliDepo.Api.Repositories;

namespace AkilliDepo.Api.Managers;

public interface IStoreManager
{
    Task<ServiceResult<PagedResponse<StoreDto>>> GetPagedAsync(PagedRequest request);
    Task<ServiceResult<StoreDto>> GetByIdAsync(int id, string? companyId);
    Task<ServiceResult<StoreDto>> CreateAsync(CreateStoreRequest request);
    Task<ServiceResult<StoreDto>> UpdateAsync(UpdateStoreRequest request);
    Task<ServiceResult<bool>> DeleteAsync(DeleteRequest request);

    /// <summary>
    /// Mağaza adına göre eşleştirir; varsa kayıtlı StoreCode'u (ve en güncel adresi) döner, yoksa yeni
    /// bir mağaza kaydı oluşturur. "IsNew" true ise çağıran taraf kullanıcıya bildirim gösterebilir.
    /// Mağaza siparişi oluşturma (elle veya PDF'ten) her zaman bu metottan geçer — StoreId artık serbest
    /// metin değil, DB'de tutulan kalıcı bir kimliktir.
    /// </summary>
    Task<(Store Store, bool IsNew)> GetOrCreateAsync(string companyId, string name, string address);
}

public class StoreManager : IStoreManager
{
    private readonly IStoreRepository _repository;
    private readonly IStoreOrderRepository _storeOrderRepository;

    public StoreManager(IStoreRepository repository, IStoreOrderRepository storeOrderRepository)
    {
        _repository = repository;
        _storeOrderRepository = storeOrderRepository;
    }

    private static StoreDto ToDto(Store s) => new()
    {
        Id = s.Id,
        CompanyId = s.CompanyId,
        StoreCode = s.StoreCode,
        Name = s.Name,
        Address = s.Address,
        CreatedAt = s.CreatedAt
    };

    /// <summary>Brand.ShortCode ile birebir aynı üretim deseni: her zaman 3 karakter.</summary>
    private async Task<string> GenerateStoreCodeAsync(string companyId, string name)
    {
        var basePrefix = BarcodeText.ToBarcodeSafeUpper(new string(name.Where(char.IsLetterOrDigit).Take(3).ToArray())).PadRight(3, 'X');
        var random = new Random();
        var candidate = basePrefix;
        // Son karakter A-Z arası değiştirilerek çakışma çözülür — bu alan yalnızca 26 kombinasyon
        // sunduğu için (aynı 2 karakterlik önek çok sayıda mağaza tarafından paylaşılırsa) maksimum
        // deneme sınırı olmadan sonsuz döngüye girebilirdi.
        for (var attempt = 0; attempt < 26 && await _repository.GetByStoreCodeAsync(companyId, candidate) is not null; attempt++)
        {
            var randomChar = (char)('A' + random.Next(0, 26));
            candidate = basePrefix[..2] + randomChar;
        }
        return candidate;
    }

    public async Task<(Store Store, bool IsNew)> GetOrCreateAsync(string companyId, string name, string address)
    {
        var existing = await _repository.GetByNameAsync(companyId, name);
        if (existing is not null)
        {
            if (!string.IsNullOrWhiteSpace(address) && existing.Address != address)
            {
                existing.Address = address;
                await _repository.UpdateAsync(existing);
                await _repository.SaveChangesAsync();
            }
            return (existing, false);
        }

        var storeCode = await GenerateStoreCodeAsync(companyId, name);
        var store = new Store
        {
            CompanyId = companyId,
            StoreCode = storeCode,
            Name = name,
            Address = address,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(store);
        await _repository.SaveChangesAsync();

        return (store, true);
    }

    public async Task<ServiceResult<PagedResponse<StoreDto>>> GetPagedAsync(PagedRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<PagedResponse<StoreDto>>.BadRequest("CompanyId zorunludur.");

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 25 : request.PageSize;

        var (items, totalCount) = await _repository.GetPagedAsync(request.CompanyId, page, pageSize, request.Search);

        return ServiceResult<PagedResponse<StoreDto>>.Ok(new PagedResponse<StoreDto>
        {
            Data = items.Select(ToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    public async Task<ServiceResult<StoreDto>> GetByIdAsync(int id, string? companyId)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return ServiceResult<StoreDto>.BadRequest("CompanyId zorunludur.");

        var store = await _repository.GetByIdAsync(id);
        if (store is null)
            return ServiceResult<StoreDto>.NotFound("Mağaza bulunamadı.");
        if (store.CompanyId != companyId)
            return ServiceResult<StoreDto>.Forbidden("Bu mağazaya erişim yetkiniz yok.");

        return ServiceResult<StoreDto>.Ok(ToDto(store));
    }

    public async Task<ServiceResult<StoreDto>> CreateAsync(CreateStoreRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<StoreDto>.BadRequest("CompanyId zorunludur.");
        if (string.IsNullOrWhiteSpace(request.Name))
            return ServiceResult<StoreDto>.BadRequest("Mağaza adı zorunludur.");
        if (string.IsNullOrWhiteSpace(request.Address))
            return ServiceResult<StoreDto>.BadRequest("Adres zorunludur.");

        var existing = await _repository.GetByNameAsync(request.CompanyId, request.Name);
        if (existing is not null)
            return ServiceResult<StoreDto>.BadRequest("Bu isimde bir mağaza zaten var.");

        var storeCode = await GenerateStoreCodeAsync(request.CompanyId, request.Name);
        var store = new Store
        {
            CompanyId = request.CompanyId,
            StoreCode = storeCode,
            Name = request.Name,
            Address = request.Address,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(store);
        await _repository.SaveChangesAsync();

        return ServiceResult<StoreDto>.Ok(ToDto(store), $"Mağaza oluşturuldu. Kod: {storeCode}");
    }

    public async Task<ServiceResult<StoreDto>> UpdateAsync(UpdateStoreRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<StoreDto>.BadRequest("CompanyId zorunludur.");
        if (string.IsNullOrWhiteSpace(request.Name))
            return ServiceResult<StoreDto>.BadRequest("Mağaza adı zorunludur.");
        if (string.IsNullOrWhiteSpace(request.Address))
            return ServiceResult<StoreDto>.BadRequest("Adres zorunludur.");

        var store = await _repository.GetByIdAsync(request.Id);
        if (store is null)
            return ServiceResult<StoreDto>.NotFound("Mağaza bulunamadı.");
        if (store.CompanyId != request.CompanyId)
            return ServiceResult<StoreDto>.Forbidden("Bu mağazaya erişim yetkiniz yok.");

        var duplicate = await _repository.GetByNameAsync(request.CompanyId, request.Name);
        if (duplicate is not null && duplicate.Id != store.Id)
            return ServiceResult<StoreDto>.BadRequest("Bu isimde bir mağaza zaten var.");

        store.Name = request.Name;
        store.Address = request.Address;

        await _repository.UpdateAsync(store);
        await _repository.SaveChangesAsync();

        return ServiceResult<StoreDto>.Ok(ToDto(store), "Mağaza güncellendi.");
    }

    public async Task<ServiceResult<bool>> DeleteAsync(DeleteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<bool>.BadRequest("CompanyId zorunludur.");

        var store = await _repository.GetByIdAsync(request.Id);
        if (store is null)
            return ServiceResult<bool>.NotFound("Mağaza bulunamadı.");
        if (store.CompanyId != request.CompanyId)
            return ServiceResult<bool>.Forbidden("Bu mağazaya erişim yetkiniz yok.");

        var hasOrders = await _storeOrderRepository.AnyByStoreCodeAsync(request.CompanyId, store.StoreCode);
        if (hasOrders)
            return ServiceResult<bool>.BadRequest("Bu mağazaya ait siparişler var, mağaza silinemez.");

        store.IsDeleted = true;
        await _repository.UpdateAsync(store);
        await _repository.SaveChangesAsync();

        return ServiceResult<bool>.Ok(true, "Mağaza silindi.");
    }
}
