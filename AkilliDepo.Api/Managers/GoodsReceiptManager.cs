using AkilliDepo.Api.DTOs;
using AkilliDepo.Api.Entities;
using AkilliDepo.Api.Repositories;

namespace AkilliDepo.Api.Managers;

public interface IGoodsReceiptManager
{
    Task<ServiceResult<PagedResponse<GoodsReceiptDto>>> GetPagedAsync(PagedRequest request);
    Task<ServiceResult<PagedResponse<GoodsReceiptItemDto>>> GetItemsPagedAsync(PagedRequest request);
    Task<ServiceResult<GoodsReceiptDto>> GetByIdAsync(int id, string? companyId);
    Task<ServiceResult<GoodsReceiptDto>> CreateSessionAsync(CreateGoodsReceiptSessionRequest request);
    Task<ServiceResult<GoodsReceiptDto>> ScanItemAsync(ScanGoodsReceiptItemRequest request);
    Task<ServiceResult<bool>> DeleteAsync(DeleteRequest request);
}

public class GoodsReceiptManager : IGoodsReceiptManager
{
    private readonly IGoodsReceiptRepository _repository;
    private readonly IProductRepository _productRepository;
    private readonly IBoxRepository _boxRepository;

    public GoodsReceiptManager(
        IGoodsReceiptRepository repository,
        IProductRepository productRepository,
        IBoxRepository boxRepository)
    {
        _repository = repository;
        _productRepository = productRepository;
        _boxRepository = boxRepository;
    }

    private static GoodsReceiptDto ToDto(GoodsReceipt r)
    {
        var activeItems = r.Items.Where(i => !i.IsDeleted).OrderBy(i => i.Id).ToList();
        var runningTotals = new Dictionary<int, int>();

        var itemDtos = activeItems.Select(i =>
        {
            runningTotals.TryGetValue(i.ProductId, out var runningTotal);
            runningTotal += i.CountedQuantity;
            runningTotals[i.ProductId] = runningTotal;

            return new GoodsReceiptItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? string.Empty,
                ProductBarcode = i.Product?.Barcode ?? string.Empty,
                ProductColor = i.Product?.Color ?? string.Empty,
                BrandId = i.BrandId,
                BrandName = i.Brand?.Name ?? string.Empty,
                BoxId = i.BoxId,
                BoxBarcode = i.Box?.Barcode ?? string.Empty,
                Desi = i.Box?.Desi,
                CountedQuantity = i.CountedQuantity,
                CumulativeQuantity = runningTotal,
                CreatedAt = i.CreatedAt
            };
        }).ToList();

        return new GoodsReceiptDto
        {
            Id = r.Id,
            CompanyId = r.CompanyId,
            ReceivedAt = r.ReceivedAt,
            Items = itemDtos
        };
    }

    public async Task<ServiceResult<PagedResponse<GoodsReceiptDto>>> GetPagedAsync(PagedRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<PagedResponse<GoodsReceiptDto>>.BadRequest("CompanyId zorunludur.");

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 25 : request.PageSize;

        var (items, totalCount) = await _repository.GetPagedAsync(
            request.CompanyId, page, pageSize, request.Search);

        return ServiceResult<PagedResponse<GoodsReceiptDto>>.Ok(new PagedResponse<GoodsReceiptDto>
        {
            Data = items.Select(ToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    public async Task<ServiceResult<PagedResponse<GoodsReceiptItemDto>>> GetItemsPagedAsync(PagedRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<PagedResponse<GoodsReceiptItemDto>>.BadRequest("CompanyId zorunludur.");

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 25 : request.PageSize;

        var (items, totalCount) = await _repository.GetItemsPagedAsync(
            request.CompanyId, page, pageSize, request.Search, request.FromDate, request.ToDate);

        var dtos = items.Select(i => new GoodsReceiptItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductName = i.Product?.Name ?? string.Empty,
            ProductBarcode = i.Product?.Barcode ?? string.Empty,
            ProductColor = i.Product?.Color ?? string.Empty,
            BrandId = i.BrandId,
            BrandName = i.Brand?.Name ?? string.Empty,
            BoxId = i.BoxId,
            BoxBarcode = i.Box?.Barcode ?? string.Empty,
            Desi = i.Box?.Desi,
            CountedQuantity = i.CountedQuantity,
            CumulativeQuantity = i.CountedQuantity,
            CreatedAt = i.CreatedAt
        }).ToList();

        return ServiceResult<PagedResponse<GoodsReceiptItemDto>>.Ok(new PagedResponse<GoodsReceiptItemDto>
        {
            Data = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    public async Task<ServiceResult<GoodsReceiptDto>> GetByIdAsync(int id, string? companyId)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return ServiceResult<GoodsReceiptDto>.BadRequest("CompanyId zorunludur.");

        var receipt = await _repository.GetByIdWithItemsAsync(id);
        if (receipt is null)
            return ServiceResult<GoodsReceiptDto>.NotFound("Mal kabul kaydı bulunamadı.");
        if (receipt.CompanyId != companyId)
            return ServiceResult<GoodsReceiptDto>.Forbidden("Bu kayda erişim yetkiniz yok.");

        return ServiceResult<GoodsReceiptDto>.Ok(ToDto(receipt));
    }

    public async Task<ServiceResult<GoodsReceiptDto>> CreateSessionAsync(CreateGoodsReceiptSessionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<GoodsReceiptDto>.BadRequest("CompanyId zorunludur.");

        var receipt = new GoodsReceipt
        {
            CompanyId = request.CompanyId,
            ReceivedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(receipt);
        await _repository.SaveChangesAsync();

        return ServiceResult<GoodsReceiptDto>.Ok(ToDto(receipt), "Mal kabul oturumu açıldı.");
    }

    private static string GenerateBoxBarcode(string createdBy, string productName)
    {
        var userPrefix = BarcodeText.ToBarcodeSafeUpper(new string(createdBy.Take(3).ToArray())).PadRight(3, 'X');
        var productPrefix = BarcodeText.ToBarcodeSafeUpper(new string(productName.Take(3).ToArray())).PadRight(3, 'X');
        var random = new Random();
        // 6 haneli rastgele kısım (~900.000 kombinasyon) — bkz. BoxManager.GenerateBarcode.
        var randomSuffix = random.Next(100000, 999999).ToString();
        return userPrefix + productPrefix + randomSuffix;
    }

    public async Task<ServiceResult<GoodsReceiptDto>> ScanItemAsync(ScanGoodsReceiptItemRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<GoodsReceiptDto>.BadRequest("CompanyId zorunludur.");
        if (string.IsNullOrWhiteSpace(request.ProductBarcode))
            return ServiceResult<GoodsReceiptDto>.BadRequest("Ürün barkodu zorunludur.");
        if (request.Quantity <= 0)
            return ServiceResult<GoodsReceiptDto>.BadRequest("Miktar sıfırdan büyük olmalıdır.");
        if (string.IsNullOrWhiteSpace(request.CreatedBy))
            return ServiceResult<GoodsReceiptDto>.BadRequest("Kullanıcı adı zorunludur.");

        var receipt = await _repository.GetByIdAsync(request.GoodsReceiptId);
        if (receipt is null)
            return ServiceResult<GoodsReceiptDto>.NotFound("Mal kabul oturumu bulunamadı.");
        if (receipt.CompanyId != request.CompanyId)
            return ServiceResult<GoodsReceiptDto>.Forbidden("Bu oturuma erişim yetkiniz yok.");

        var product = await _productRepository.GetByBarcodeAsync(request.CompanyId, request.ProductBarcode);
        if (product is null)
            return ServiceResult<GoodsReceiptDto>.NotFound("Bu barkoda sahip ürün bulunamadı. Önce ürünü tanımlayın.");

        string? boxBarcode = null;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var candidate = GenerateBoxBarcode(request.CreatedBy, product.Name);
            if (await _boxRepository.GetByBarcodeAsync(request.CompanyId, candidate) is null)
            {
                boxBarcode = candidate;
                break;
            }
        }
        if (boxBarcode is null)
            return ServiceResult<GoodsReceiptDto>.BadRequest("Bu kullanıcı ve ürün için eşsiz bir koli barkodu üretilemedi, lütfen tekrar deneyin.");

        // Tek SaveChangesAsync çağrısı: koli + mal kabul kalemi tek transaction'da yazılır
        var now = DateTime.UtcNow;
        var box = new Box
        {
            CompanyId = request.CompanyId,
            Barcode = boxBarcode,
            ProductId = product.Id,
            Quantity = request.Quantity,
            Desi = request.Desi,
            Status = BoxStatus.InStock,
            CreatedBy = request.CreatedBy,
            ProductColor = product.Color,
            CreatedAt = now
        };
        await _boxRepository.AddAsync(box);

        var item = new GoodsReceiptItem
        {
            CompanyId = request.CompanyId,
            GoodsReceiptId = receipt.Id,
            ProductId = product.Id,
            BrandId = product.BrandId,
            CountedQuantity = request.Quantity,
            Box = box,
            CreatedAt = now
        };
        await _repository.AddItemAsync(item);

        // GoodsReceipt'te CreatedBy'ı set et
        receipt.CreatedBy = request.CreatedBy;

        await _repository.SaveChangesAsync();

        var updated = await _repository.GetByIdWithItemsAsync(receipt.Id);
        return ServiceResult<GoodsReceiptDto>.Ok(ToDto(updated!), $"Ürün kabul edildi, koli oluşturuldu. Koli Barkodu: {boxBarcode}");
    }

    public async Task<ServiceResult<bool>> DeleteAsync(DeleteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<bool>.BadRequest("CompanyId zorunludur.");

        var receipt = await _repository.GetByIdWithItemsAsync(request.Id);
        if (receipt is null)
            return ServiceResult<bool>.NotFound("Mal kabul kaydı bulunamadı.");
        if (receipt.CompanyId != request.CompanyId)
            return ServiceResult<bool>.Forbidden("Bu kayda erişim yetkiniz yok.");

        // Fiziksel koliler depoda kalmaya devam eder; yalnızca oturum kaydı arşivlenir
        receipt.IsDeleted = true;
        foreach (var item in receipt.Items)
            item.IsDeleted = true;

        await _repository.SaveChangesAsync();

        return ServiceResult<bool>.Ok(true, "Mal kabul kaydı silindi.");
    }
}
