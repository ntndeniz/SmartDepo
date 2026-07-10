using AkilliDepo.Api.DTOs;
using AkilliDepo.Api.Entities;
using AkilliDepo.Api.Repositories;
using AkilliDepo.Api.Services;

namespace AkilliDepo.Api.Managers;

public interface IStoreOrderManager
{
    Task<ServiceResult<PagedResponse<StoreOrderDto>>> GetPagedAsync(PagedRequest request);
    Task<ServiceResult<StoreOrderDto>> GetByCodeAsync(string? companyId, string? orderCode);
    Task<ServiceResult<StoreOrderDto>> CreateAsync(CreateStoreOrderRequest request);
    Task<ServiceResult<ParsedStoreOrderDto>> ParsePdfAsync(string? companyId, Stream pdfStream);
}

public class StoreOrderManager : IStoreOrderManager
{
    private readonly IStoreOrderRepository _repository;
    private readonly IProductRepository _productRepository;
    private readonly IPdfOrderParser _pdfParser;
    private readonly IStoreManager _storeManager;
    private readonly IDispatchRepository _dispatchRepository;
    private readonly IDispatchPalletRepository _palletRepository;

    public StoreOrderManager(
        IStoreOrderRepository repository,
        IProductRepository productRepository,
        IPdfOrderParser pdfParser,
        IStoreManager storeManager,
        IDispatchRepository dispatchRepository,
        IDispatchPalletRepository palletRepository)
    {
        _repository = repository;
        _productRepository = productRepository;
        _pdfParser = pdfParser;
        _storeManager = storeManager;
        _dispatchRepository = dispatchRepository;
        _palletRepository = palletRepository;
    }

    private static StoreOrderDto ToDto(StoreOrder o) => new()
    {
        Id = o.Id,
        CompanyId = o.CompanyId,
        OrderCode = o.OrderCode,
        StoreId = o.StoreId,
        StoreName = o.StoreName,
        Address = o.Address,
        CreatedAt = o.CreatedAt,
        Items = o.Items.Where(i => !i.IsDeleted).Select(i => new StoreOrderItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductBarcode = i.Product?.Barcode ?? string.Empty,
            ProductName = i.Product?.Name ?? string.Empty,
            Color = i.Color,
            Quantity = i.Quantity
        }).ToList()
    };

    // ÖNEMLİ: prefix, StoreManager.GetOrCreateAsync'in çözümlediği gerçek store.StoreCode ile
    // çağrılmalıdır — ham mağaza adından tekrar hesaplanırsa, kod çakışması durumunda StoreManager'ın
    // atadığı farklı kod (ör. "TEY") ile burada üretilen önek (ör. "TES") tutarsız kalır ve sipariş
    // barkodu başka bir mağazanın koduna işaret ediyormuş gibi okunur (canlı testte doğrulanan bug).
    private static string GenerateOrderCode(string storeCode)
    {
        var prefix = BarcodeText.ToBarcodeSafeUpper(storeCode).PadRight(3, 'X')[..3];
        var random = new Random();
        return "SO" + prefix + random.Next(1000, 9999);
    }

    public async Task<ServiceResult<PagedResponse<StoreOrderDto>>> GetPagedAsync(PagedRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<PagedResponse<StoreOrderDto>>.BadRequest("CompanyId zorunludur.");

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 25 : request.PageSize;

        var (items, totalCount) = await _repository.GetPagedAsync(
            request.CompanyId, page, pageSize, request.Search, request.FromDate, request.ToDate);

        var statuses = await _dispatchRepository.GetStatusesByStoreOrderIdsAsync(
            request.CompanyId, items.Select(i => i.Id).ToList());
        // GroupBy + en yeni (en yüksek DispatchOrderId) kullanılır: normalde bir StoreOrder'ın tek bir
        // DispatchOrder'ı olur, ama bu düzeltme öncesi oluşmuş yinelenen kayıtlar varsa (bkz. "aynı
        // sipariş barkodu birden fazla kez okutulunca yinelenen dağıtım emri" düzeltmesi)
        // ToDictionary burada "aynı anahtar iki kez eklenemez" istisnasıyla 500 hatası veriyordu.
        var statusMap = statuses
            .GroupBy(s => s.StoreOrderId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(s => s.DispatchOrderId).First());

        var dtos = items.Select(ToDto).ToList();
        foreach (var dto in dtos)
        {
            if (statusMap.TryGetValue(dto.Id, out var lookup))
            {
                dto.DispatchStatus = lookup.Status;
                dto.DispatchOrderId = lookup.DispatchOrderId;
            }
        }

        // "Completed" durumundaki emirler için, kolileri paletlenip sevkiyata onaylandıysa/sevk
        // edildiyse Siparişler ekranında daha bilgilendirici bir durum göster.
        var completedOrderIds = dtos.Where(d => d.DispatchStatus == "Completed" && d.DispatchOrderId.HasValue)
            .Select(d => d.DispatchOrderId!.Value).ToList();
        if (completedOrderIds.Count > 0)
        {
            var rollup = await _palletRepository.GetOrderRollupStatusesAsync(request.CompanyId, completedOrderIds);
            foreach (var dto in dtos)
            {
                if (dto.DispatchOrderId.HasValue && rollup.TryGetValue(dto.DispatchOrderId.Value, out var rolledUp))
                {
                    dto.DispatchStatus = rolledUp;
                }
            }
        }

        return ServiceResult<PagedResponse<StoreOrderDto>>.Ok(new PagedResponse<StoreOrderDto>
        {
            Data = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    public async Task<ServiceResult<StoreOrderDto>> GetByCodeAsync(string? companyId, string? orderCode)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return ServiceResult<StoreOrderDto>.BadRequest("CompanyId zorunludur.");
        if (string.IsNullOrWhiteSpace(orderCode))
            return ServiceResult<StoreOrderDto>.BadRequest("Sipariş barkodu zorunludur.");

        var order = await _repository.GetByCodeAsync(companyId, orderCode);
        if (order is null)
            return ServiceResult<StoreOrderDto>.NotFound("Bu barkoda sahip mağaza siparişi bulunamadı.");

        return ServiceResult<StoreOrderDto>.Ok(ToDto(order));
    }

    public async Task<ServiceResult<StoreOrderDto>> CreateAsync(CreateStoreOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<StoreOrderDto>.BadRequest("CompanyId zorunludur.");
        if (string.IsNullOrWhiteSpace(request.StoreName))
            return ServiceResult<StoreOrderDto>.BadRequest("Mağaza adı zorunludur.");
        if (string.IsNullOrWhiteSpace(request.Address))
            return ServiceResult<StoreOrderDto>.BadRequest("Adres zorunludur.");
        if (request.Items.Count == 0)
            return ServiceResult<StoreOrderDto>.BadRequest("En az bir ürün eklemelisiniz.");

        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
                return ServiceResult<StoreOrderDto>.BadRequest("Miktar sıfırdan büyük olmalıdır.");

            var product = await _productRepository.GetByIdAsync(item.ProductId);
            if (product is null || product.CompanyId != request.CompanyId)
                return ServiceResult<StoreOrderDto>.BadRequest($"Ürün bulunamadı (Id: {item.ProductId}).");
        }

        // Mağaza kimliği artık serbest metin değil: isme göre eşleştirilip kalıcı Store kaydından
        // (StoreCode) alınır. Aynı mağaza tekrar sipariş verdiğinde her zaman aynı kod kullanılır.
        var (store, isNewStore) = await _storeManager.GetOrCreateAsync(request.CompanyId, request.StoreName, request.Address);

        string? orderCode = null;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var candidate = GenerateOrderCode(store.StoreCode);
            if (await _repository.GetByCodeAsync(request.CompanyId, candidate) is null)
            {
                orderCode = candidate;
                break;
            }
        }
        if (orderCode is null)
            return ServiceResult<StoreOrderDto>.BadRequest("Eşsiz bir sipariş barkodu üretilemedi, lütfen tekrar deneyin.");

        var order = new StoreOrder
        {
            CompanyId = request.CompanyId,
            OrderCode = orderCode,
            StoreId = store.StoreCode,
            StoreName = store.Name,
            Address = store.Address,
            CreatedAt = DateTime.UtcNow,
            Items = request.Items.Select(i => new StoreOrderItem
            {
                CompanyId = request.CompanyId,
                ProductId = i.ProductId,
                Color = i.Color ?? string.Empty,
                Quantity = i.Quantity
            }).ToList()
        };

        await _repository.AddAsync(order);
        await _repository.SaveChangesAsync();

        var created = await _repository.GetByIdAsync(order.Id);
        var message = $"Mağaza siparişi oluşturuldu. Sipariş Barkodu: {orderCode}";
        if (isNewStore)
            message += $" YENİ MAĞAZA EKLENDİ: {store.Name} (Kod: {store.StoreCode}).";

        return ServiceResult<StoreOrderDto>.Ok(ToDto(created!), message);
    }

    public async Task<ServiceResult<ParsedStoreOrderDto>> ParsePdfAsync(string? companyId, Stream pdfStream)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return ServiceResult<ParsedStoreOrderDto>.BadRequest("CompanyId zorunludur.");

        var products = await _productRepository.GetAllActiveAsync(companyId);

        ParsedStoreOrderDto parsed;
        try
        {
            parsed = _pdfParser.Parse(pdfStream, products);
        }
        catch (Exception)
        {
            return ServiceResult<ParsedStoreOrderDto>.BadRequest(
                "PDF okunamadı. Dosyanın bozuk olmadığından ve metin içerdiğinden (taranmış görüntü değil) emin olun.");
        }

        return ServiceResult<ParsedStoreOrderDto>.Ok(parsed, "PDF ayrıştırıldı, lütfen onaylamadan önce kontrol edin.");
    }
}
