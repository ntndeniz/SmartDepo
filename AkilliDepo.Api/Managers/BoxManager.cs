using AkilliDepo.Api.DTOs;
using AkilliDepo.Api.Entities;
using AkilliDepo.Api.Repositories;

namespace AkilliDepo.Api.Managers;

public interface IBoxManager
{
    Task<ServiceResult<PagedResponse<BoxDto>>> GetPagedAsync(BoxPagedRequest request);
    Task<ServiceResult<BoxDto>> GetByIdAsync(int id, string? companyId);
    Task<ServiceResult<BoxDto>> CreateAsync(CreateBoxRequest request);
    Task<ServiceResult<BoxDto>> UpdateAsync(UpdateBoxRequest request);
    Task<ServiceResult<bool>> DeleteAsync(DeleteRequest request);
}

public class BoxManager : IBoxManager
{
    private readonly IBoxRepository _repository;
    private readonly IProductRepository _productRepository;
    private readonly IStockAdjustmentRepository _adjustmentRepository;
    private readonly ILocationRepository _locationRepository;

    public BoxManager(
        IBoxRepository repository,
        IProductRepository productRepository,
        IStockAdjustmentRepository adjustmentRepository,
        ILocationRepository locationRepository)
    {
        _repository = repository;
        _productRepository = productRepository;
        _adjustmentRepository = adjustmentRepository;
        _locationRepository = locationRepository;
    }

    private static BoxDto ToDto(Box b) => new()
    {
        Id = b.Id,
        CompanyId = b.CompanyId,
        Barcode = b.Barcode,
        ProductId = b.ProductId,
        ProductName = b.Product?.Name ?? string.Empty,
        ProductBarcode = b.Product?.Barcode ?? string.Empty,
        ProductColor = b.ProductColor,
        Quantity = b.Quantity,
        Desi = b.Desi,
        Status = b.Status,
        CreatedBy = b.CreatedBy,
        CreatedAt = b.CreatedAt
    };

    public async Task<ServiceResult<PagedResponse<BoxDto>>> GetPagedAsync(BoxPagedRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<PagedResponse<BoxDto>>.BadRequest("CompanyId zorunludur.");

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 25 : request.PageSize;

        var (items, totalCount) = await _repository.GetPagedAsync(
            request.CompanyId, page, pageSize, request.Search, request.Status);

        return ServiceResult<PagedResponse<BoxDto>>.Ok(new PagedResponse<BoxDto>
        {
            Data = items.Select(ToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    public async Task<ServiceResult<BoxDto>> GetByIdAsync(int id, string? companyId)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return ServiceResult<BoxDto>.BadRequest("CompanyId zorunludur.");

        var box = await _repository.GetByIdAsync(id);
        if (box is null)
            return ServiceResult<BoxDto>.NotFound("Koli bulunamadı.");
        if (box.CompanyId != companyId)
            return ServiceResult<BoxDto>.Forbidden("Bu koliye erişim yetkiniz yok.");

        return ServiceResult<BoxDto>.Ok(ToDto(box));
    }

    private static string GenerateBarcode(string createdBy, string productName)
    {
        var userPrefix = BarcodeText.ToBarcodeSafeUpper(new string(createdBy.Take(3).ToArray())).PadRight(3, 'X');
        var productPrefix = BarcodeText.ToBarcodeSafeUpper(new string(productName.Take(3).ToArray())).PadRight(3, 'X');
        var random = new Random();
        // 6 haneli rastgele kısım (~900.000 kombinasyon) — aynı kullanıcı+ürün için önceki 3 haneli
        // (~900 kombinasyon) alan, günlük yoğun kabul senaryosunda aylar içinde tükenebiliyordu.
        var randomSuffix = random.Next(100000, 999999).ToString();
        return userPrefix + productPrefix + randomSuffix;
    }

    public async Task<ServiceResult<BoxDto>> CreateAsync(CreateBoxRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<BoxDto>.BadRequest("CompanyId zorunludur.");
        if (string.IsNullOrWhiteSpace(request.CreatedBy))
            return ServiceResult<BoxDto>.BadRequest("Kullanıcı adı zorunludur.");
        if (request.Quantity <= 0)
            return ServiceResult<BoxDto>.BadRequest("Miktar sıfırdan büyük olmalıdır.");

        var product = await _productRepository.GetByIdAsync(request.ProductId);
        if (product is null || product.CompanyId != request.CompanyId)
            return ServiceResult<BoxDto>.BadRequest("Ürün bulunamadı.");

        string? barcode = null;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var candidate = GenerateBarcode(request.CreatedBy, product.Name);
            if (await _repository.GetByBarcodeAsync(request.CompanyId, candidate) is null)
            {
                barcode = candidate;
                break;
            }
        }
        if (barcode is null)
            return ServiceResult<BoxDto>.BadRequest("Bu kullanıcı ve ürün için eşsiz bir koli barkodu üretilemedi, lütfen tekrar deneyin.");

        // Henüz bir palete ait olmayan koli InStock başlar
        var box = new Box
        {
            CompanyId = request.CompanyId,
            Barcode = barcode,
            ProductId = request.ProductId,
            Quantity = request.Quantity,
            Desi = request.Desi,
            Status = BoxStatus.InStock,
            CreatedBy = request.CreatedBy,
            ProductColor = product.Color,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(box);
        await _repository.SaveChangesAsync();

        var created = await _repository.GetByIdAsync(box.Id);
        return ServiceResult<BoxDto>.Ok(ToDto(created!), $"Koli oluşturuldu. Barkod: {barcode}");
    }

    public async Task<ServiceResult<BoxDto>> UpdateAsync(UpdateBoxRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<BoxDto>.BadRequest("CompanyId zorunludur.");
        if (request.Quantity < 0)
            return ServiceResult<BoxDto>.BadRequest("Miktar negatif olamaz.");

        var box = await _repository.GetByIdAsync(request.Id);
        if (box is null)
            return ServiceResult<BoxDto>.NotFound("Koli bulunamadı.");
        if (box.CompanyId != request.CompanyId)
            return ServiceResult<BoxDto>.Forbidden("Bu koliye erişim yetkiniz yok.");

        var quantityChanged = box.Quantity != request.Quantity;
        if (quantityChanged && string.IsNullOrWhiteSpace(request.Reason))
            return ServiceResult<BoxDto>.BadRequest(
                "Koli miktarını değiştirmek için bir gerekçe girmelisiniz (sayım farkı, fire, hasar vb.).");

        var oldQuantity = box.Quantity;
        box.Quantity = request.Quantity;
        box.Desi = request.Desi;

        // Miktarı 0'a düşen bir koli mantıken artık depoda yok — sessizce "boş koli" olarak listede
        // kalmak yerine (silinen kolilerle aynı mantık) otomatik soft-delete edilir ve rafı varsa
        // boşaltılır. İSTİSNA: koli daha önce bir sevkiyata kaynak olarak kullanıldıysa (kısmi toplama
        // sonrası kalan miktar 0'a çekildi) soft-delete edilmez — SourceBoxId non-nullable FK olduğu
        // için silinirse o geçmiş sevkiyat kaydı Include (INNER JOIN) sorgusundan sessizce düşer.
        var justEmptied = request.Quantity == 0 && !box.IsDeleted
            && !await _repository.IsReferencedAsDispatchSourceAsync(box.Id);
        if (justEmptied)
        {
            await ReleaseLocationIfAnyAsync(box.Id);
            box.IsDeleted = true;
        }

        await _repository.UpdateAsync(box);

        if (quantityChanged)
        {
            await _adjustmentRepository.AddAsync(new StockAdjustment
            {
                CompanyId = request.CompanyId,
                BoxId = box.Id,
                OldQuantity = oldQuantity,
                NewQuantity = request.Quantity,
                Reason = request.Reason!,
                AdjustedBy = string.IsNullOrWhiteSpace(request.AdjustedBy) ? "bilinmiyor" : request.AdjustedBy,
                AdjustedAt = DateTime.UtcNow
            });
        }

        await _repository.SaveChangesAsync();

        var message = justEmptied
            ? "Koli güncellendi. Miktar 0'a düştüğü için koli otomatik olarak kaldırıldı."
            : "Koli güncellendi.";
        return ServiceResult<BoxDto>.Ok(ToDto(box), message);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(DeleteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<bool>.BadRequest("CompanyId zorunludur.");

        var box = await _repository.GetByIdAsync(request.Id);
        if (box is null)
            return ServiceResult<bool>.NotFound("Koli bulunamadı.");
        if (box.CompanyId != request.CompanyId)
            return ServiceResult<bool>.Forbidden("Bu koliye erişim yetkiniz yok.");

        await ReleaseLocationIfAnyAsync(box.Id);

        box.IsDeleted = true;
        await _repository.UpdateAsync(box);
        await _repository.SaveChangesAsync();

        return ServiceResult<bool>.Ok(true, "Koli silindi.");
    }

    // Koli bir rafa yerleştirilmişse boşaltır — aksi halde raf kalıcı olarak "dolu" ama artık var
    // olmayan/boşalmış bir koliye işaret eden bozuk (orphan) durumda kalır.
    private async Task ReleaseLocationIfAnyAsync(int boxId)
    {
        var location = await _locationRepository.GetByCurrentBoxIdAsync(boxId);
        if (location is not null)
        {
            location.IsOccupied = false;
            location.CurrentBoxId = null;
            await _locationRepository.UpdateAsync(location);
        }
    }
}
