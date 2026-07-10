using AkilliDepo.Api.DTOs;
using AkilliDepo.Api.Entities;
using AkilliDepo.Api.Repositories;

namespace AkilliDepo.Api.Managers;

public interface IBrandManager
{
    Task<ServiceResult<PagedResponse<BrandDto>>> GetPagedAsync(PagedRequest request);
    Task<ServiceResult<BrandDto>> GetByIdAsync(int id, string? companyId);
    Task<ServiceResult<BrandDto>> CreateAsync(CreateBrandRequest request);
    Task<ServiceResult<BrandDto>> UpdateAsync(UpdateBrandRequest request);
    Task<ServiceResult<bool>> DeleteAsync(DeleteRequest request);
}

public class BrandManager : IBrandManager
{
    private readonly IBrandRepository _repository;
    private readonly IProductRepository _productRepository;

    public BrandManager(IBrandRepository repository, IProductRepository productRepository)
    {
        _repository = repository;
        _productRepository = productRepository;
    }

    private static BrandDto ToDto(Brand b) => new()
    {
        Id = b.Id,
        CompanyId = b.CompanyId,
        Name = b.Name,
        ShortCode = b.ShortCode,
        CreatedAt = b.CreatedAt
    };

    private async Task<string> GenerateShortCodeAsync(string companyId, string name)
    {
        var basePrefix = BarcodeText.ToBarcodeSafeUpper(new string(name.Where(char.IsLetterOrDigit).Take(3).ToArray())).PadRight(3, 'X');
        var random = new Random();
        var candidate = basePrefix;
        // Bkz. StoreManager.GenerateStoreCodeAsync — aynı sınırlı (26 kombinasyon) alan riski için
        // maksimum deneme sınırı.
        for (var attempt = 0; attempt < 26 && await _repository.GetByShortCodeAsync(companyId, candidate) is not null; attempt++)
        {
            var randomChar = (char)('A' + random.Next(0, 26));
            candidate = basePrefix[..2] + randomChar;
        }
        return candidate;
    }

    public async Task<ServiceResult<PagedResponse<BrandDto>>> GetPagedAsync(PagedRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<PagedResponse<BrandDto>>.BadRequest("CompanyId zorunludur.");

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 25 : request.PageSize;

        var (items, totalCount) = await _repository.GetPagedAsync(
            request.CompanyId, page, pageSize, request.Search);

        return ServiceResult<PagedResponse<BrandDto>>.Ok(new PagedResponse<BrandDto>
        {
            Data = items.Select(ToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    public async Task<ServiceResult<BrandDto>> GetByIdAsync(int id, string? companyId)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return ServiceResult<BrandDto>.BadRequest("CompanyId zorunludur.");

        var brand = await _repository.GetByIdAsync(id);
        if (brand is null)
            return ServiceResult<BrandDto>.NotFound("Marka bulunamadı.");
        if (brand.CompanyId != companyId)
            return ServiceResult<BrandDto>.Forbidden("Bu markaya erişim yetkiniz yok.");

        return ServiceResult<BrandDto>.Ok(ToDto(brand));
    }

    public async Task<ServiceResult<BrandDto>> CreateAsync(CreateBrandRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<BrandDto>.BadRequest("CompanyId zorunludur.");
        if (string.IsNullOrWhiteSpace(request.Name))
            return ServiceResult<BrandDto>.BadRequest("Marka adı zorunludur.");

        var existing = await _repository.GetByNameAsync(request.CompanyId, request.Name);
        if (existing is not null)
            return ServiceResult<BrandDto>.BadRequest("Bu isimde bir marka zaten var.");

        var shortCode = await GenerateShortCodeAsync(request.CompanyId, request.Name);

        var brand = new Brand
        {
            CompanyId = request.CompanyId,
            Name = request.Name,
            ShortCode = shortCode,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(brand);
        await _repository.SaveChangesAsync();

        return ServiceResult<BrandDto>.Ok(ToDto(brand), "Marka oluşturuldu.");
    }

    public async Task<ServiceResult<BrandDto>> UpdateAsync(UpdateBrandRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<BrandDto>.BadRequest("CompanyId zorunludur.");
        if (string.IsNullOrWhiteSpace(request.Name))
            return ServiceResult<BrandDto>.BadRequest("Marka adı zorunludur.");

        var brand = await _repository.GetByIdAsync(request.Id);
        if (brand is null)
            return ServiceResult<BrandDto>.NotFound("Marka bulunamadı.");
        if (brand.CompanyId != request.CompanyId)
            return ServiceResult<BrandDto>.Forbidden("Bu markaya erişim yetkiniz yok.");

        var duplicate = await _repository.GetByNameAsync(request.CompanyId, request.Name);
        if (duplicate is not null && duplicate.Id != brand.Id)
            return ServiceResult<BrandDto>.BadRequest("Bu isimde bir marka zaten var.");

        brand.Name = request.Name;

        await _repository.UpdateAsync(brand);
        await _repository.SaveChangesAsync();

        return ServiceResult<BrandDto>.Ok(ToDto(brand), "Marka güncellendi.");
    }

    public async Task<ServiceResult<bool>> DeleteAsync(DeleteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<bool>.BadRequest("CompanyId zorunludur.");

        var brand = await _repository.GetByIdAsync(request.Id);
        if (brand is null)
            return ServiceResult<bool>.NotFound("Marka bulunamadı.");
        if (brand.CompanyId != request.CompanyId)
            return ServiceResult<bool>.Forbidden("Bu markaya erişim yetkiniz yok.");

        var hasProducts = await _productRepository.AnyByBrandAsync(request.CompanyId, brand.Id);
        if (hasProducts)
            return ServiceResult<bool>.BadRequest("Bu markaya bağlı ürünler var. Önce ürünleri silin veya başka markaya taşıyın.");

        brand.IsDeleted = true;
        await _repository.UpdateAsync(brand);
        await _repository.SaveChangesAsync();

        return ServiceResult<bool>.Ok(true, "Marka silindi.");
    }
}
