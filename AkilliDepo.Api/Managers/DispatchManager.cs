using AkilliDepo.Api.DTOs;
using AkilliDepo.Api.Entities;
using AkilliDepo.Api.Repositories;

namespace AkilliDepo.Api.Managers;

public interface IDispatchManager
{
    Task<ServiceResult<PagedResponse<DispatchOrderDto>>> GetPagedAsync(DispatchPagedRequest request);
    Task<ServiceResult<DispatchOrderDto>> GetByIdAsync(int id, string? companyId);
    Task<ServiceResult<DispatchOrderDto>> CreateFromStoreOrderAsync(CreateFromStoreOrderRequest request);
    Task<ServiceResult<DispatchOrderDto>> CloseBoxAsync(CloseDispatchBoxRequest request);
    Task<ServiceResult<DispatchOrderDto>> CompleteAsync(CompleteDispatchOrderRequest request);
    Task<ServiceResult<DispatchPalletDto>> CreatePalletAsync(CreateDispatchPalletRequest request);
    Task<ServiceResult<DispatchPalletDto>> GetPalletByBarcodeAsync(string? companyId, string? barcode);
    Task<ServiceResult<PagedResponse<DispatchPalletDto>>> GetPalletsPagedAsync(DispatchPalletPagedRequest request);
    Task<ServiceResult<List<UnpalletizedBoxDto>>> GetUnpalletizedBoxesAsync(string? companyId);
    Task<ServiceResult<DispatchPalletDto>> AddBoxToPalletAsync(AddBoxToPalletRequest request);
    Task<ServiceResult<DispatchPalletDto>> RemoveBoxFromPalletAsync(RemoveBoxFromPalletRequest request);
    Task<ServiceResult<DispatchPalletDto>> MarkPalletReadyAsync(PalletActionRequest request);
    Task<ServiceResult<DispatchPalletDto>> MarkPalletShippedAsync(PalletActionRequest request);
}

public class DispatchManager : IDispatchManager
{
    private readonly IDispatchRepository _repository;
    private readonly IStoreOrderRepository _storeOrderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IBoxRepository _boxRepository;
    private readonly IDispatchPalletRepository _palletRepository;
    private readonly ILocationRepository _locationRepository;

    public DispatchManager(
        IDispatchRepository repository,
        IStoreOrderRepository storeOrderRepository,
        IProductRepository productRepository,
        IBoxRepository boxRepository,
        IDispatchPalletRepository palletRepository,
        ILocationRepository locationRepository)
    {
        _repository = repository;
        _storeOrderRepository = storeOrderRepository;
        _productRepository = productRepository;
        _boxRepository = boxRepository;
        _palletRepository = palletRepository;
        _locationRepository = locationRepository;
    }

    private static DispatchOrderDto ToDto(DispatchOrder o) => new()
    {
        Id = o.Id,
        CompanyId = o.CompanyId,
        StoreOrderId = o.StoreOrderId,
        StoreOrderCode = o.StoreOrder?.OrderCode ?? string.Empty,
        StoreId = o.StoreId,
        StoreName = o.StoreName,
        Address = o.Address,
        Status = o.Status,
        CreatedBy = o.CreatedBy,
        CreatedAt = o.CreatedAt,
        Items = o.Items.Where(i => !i.IsDeleted).Select(i => new DispatchOrderItemDto
        {
            Id = i.Id,
            ProductId = i.ProductId,
            ProductBarcode = i.Product?.Barcode ?? string.Empty,
            ProductName = i.Product?.Name ?? string.Empty,
            Color = i.Color,
            RequestedQuantity = i.RequestedQuantity,
            PickedQuantity = i.PickedQuantity
        }).ToList(),
        Boxes = o.Boxes.Where(b => !b.IsDeleted).Select(b => new DispatchBoxDto
        {
            Id = b.Id,
            DispatchOrderId = b.DispatchOrderId,
            Barcode = b.Barcode,
            CreatedBy = b.CreatedBy,
            CreatedAt = b.CreatedAt,
            Items = b.Items.Where(i => !i.IsDeleted).Select(i => new DispatchBoxItemDto
            {
                Id = i.Id,
                SourceBoxId = i.SourceBoxId,
                SourceBoxBarcode = i.SourceBox?.Barcode ?? string.Empty,
                ProductId = i.ProductId,
                ProductName = i.Product?.Name ?? string.Empty,
                Quantity = i.Quantity,
                PickedFromLocationBarcode = i.PickedFromLocationBarcode
            }).ToList()
        }).ToList()
    };

    /// <summary>Toplanacak kalemler için "nerede bulunur" önerilerini doldurur (yalnızca hâlâ eksik miktarı olan kalemler için sorgu yapılır).</summary>
    private async Task AttachPickSuggestionsAsync(DispatchOrderDto dto)
    {
        if (dto.Status == DispatchOrderStatus.Completed)
            return;

        foreach (var item in dto.Items)
        {
            if (item.PickedQuantity >= item.RequestedQuantity)
                continue;

            var suggestions = await _boxRepository.GetPickSuggestionsAsync(dto.CompanyId, item.ProductId);
            item.Suggestions = suggestions.Select(s => new PickSuggestionDto
            {
                BoxBarcode = s.BoxBarcode,
                AvailableQuantity = s.AvailableQuantity,
                Status = s.Status,
                LocationBarcode = s.LocationBarcode
            }).ToList();
        }
    }

    private static string GenerateBoxBarcode(string createdBy, string productName)
    {
        var userPrefix = BarcodeText.ToBarcodeSafeUpper(new string(createdBy.Take(3).ToArray())).PadRight(3, 'X');
        var productPrefix = BarcodeText.ToBarcodeSafeUpper(new string(productName.Take(3).ToArray())).PadRight(3, 'X');
        var random = new Random();
        // 6 haneli rastgele kısım (~900.000 kombinasyon) — bkz. BoxManager.GenerateBarcode.
        return userPrefix + productPrefix + random.Next(100000, 999999);
    }

    private static string GeneratePalletBarcode(string createdBy)
    {
        var userPrefix = BarcodeText.ToBarcodeSafeUpper(new string(createdBy.Take(3).ToArray())).PadRight(3, 'X');
        var random = new Random();
        return userPrefix + random.Next(100000, 999999);
    }

    public async Task<ServiceResult<PagedResponse<DispatchOrderDto>>> GetPagedAsync(DispatchPagedRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<PagedResponse<DispatchOrderDto>>.BadRequest("CompanyId zorunludur.");

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 25 : request.PageSize;

        var (items, totalCount) = await _repository.GetPagedAsync(
            request.CompanyId, page, pageSize, request.Search, request.Status, request.FromDate, request.ToDate);

        var dtos = items.Select(ToDto).ToList();
        foreach (var dto in dtos)
            await AttachPickSuggestionsAsync(dto);

        return ServiceResult<PagedResponse<DispatchOrderDto>>.Ok(new PagedResponse<DispatchOrderDto>
        {
            Data = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    public async Task<ServiceResult<DispatchOrderDto>> GetByIdAsync(int id, string? companyId)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return ServiceResult<DispatchOrderDto>.BadRequest("CompanyId zorunludur.");

        var order = await _repository.GetByIdWithDetailsAsync(id);
        if (order is null)
            return ServiceResult<DispatchOrderDto>.NotFound("Dağıtım emri bulunamadı.");
        if (order.CompanyId != companyId)
            return ServiceResult<DispatchOrderDto>.Forbidden("Bu dağıtım emrine erişim yetkiniz yok.");

        var dto = ToDto(order);
        await AttachPickSuggestionsAsync(dto);
        return ServiceResult<DispatchOrderDto>.Ok(dto);
    }

    public async Task<ServiceResult<DispatchOrderDto>> CreateFromStoreOrderAsync(CreateFromStoreOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<DispatchOrderDto>.BadRequest("CompanyId zorunludur.");
        if (string.IsNullOrWhiteSpace(request.StoreOrderCode))
            return ServiceResult<DispatchOrderDto>.BadRequest("Sipariş barkodu zorunludur.");
        if (string.IsNullOrWhiteSpace(request.CreatedBy))
            return ServiceResult<DispatchOrderDto>.BadRequest("Kullanıcı adı zorunludur.");

        var storeOrder = await _storeOrderRepository.GetByCodeAsync(request.CompanyId, request.StoreOrderCode);
        if (storeOrder is null)
            return ServiceResult<DispatchOrderDto>.NotFound("Bu barkoda sahip mağaza siparişi bulunamadı.");

        // Aynı sipariş barkodu birden fazla kez okutulursa (çift tarama, çift tıklama) yinelenen bir
        // dağıtım emri oluşturmak yerine zaten var olanı döndürüyoruz — kullanıcı toplama ekranında
        // aynı siparişin iki kez düşmesini görmesin.
        var existing = await _repository.GetByStoreOrderIdAsync(request.CompanyId, storeOrder.Id);
        if (existing is not null)
        {
            var existingDto = ToDto(existing);
            await AttachPickSuggestionsAsync(existingDto);
            return ServiceResult<DispatchOrderDto>.Ok(existingDto,
                "Bu sipariş için toplama zaten başlatılmış — mevcut dağıtım emri açıldı.");
        }

        var order = new DispatchOrder
        {
            CompanyId = request.CompanyId,
            StoreOrderId = storeOrder.Id,
            StoreId = storeOrder.StoreId,
            StoreName = storeOrder.StoreName,
            Address = storeOrder.Address,
            Status = DispatchOrderStatus.Picking,
            CreatedBy = request.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            Items = storeOrder.Items.Where(i => !i.IsDeleted).Select(i => new DispatchOrderItem
            {
                CompanyId = request.CompanyId,
                ProductId = i.ProductId,
                Color = i.Color,
                RequestedQuantity = i.Quantity,
                PickedQuantity = 0
            }).ToList()
        };

        await _repository.AddAsync(order);
        await _repository.SaveChangesAsync();

        var created = await _repository.GetByIdWithDetailsAsync(order.Id);
        var createdDto = ToDto(created!);
        await AttachPickSuggestionsAsync(createdDto);
        return ServiceResult<DispatchOrderDto>.Ok(createdDto, "Dağıtım emri oluşturuldu, toplama listesi hazır.");
    }

    public async Task<ServiceResult<DispatchOrderDto>> CloseBoxAsync(CloseDispatchBoxRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<DispatchOrderDto>.BadRequest("CompanyId zorunludur.");
        if (string.IsNullOrWhiteSpace(request.CreatedBy))
            return ServiceResult<DispatchOrderDto>.BadRequest("Kullanıcı adı zorunludur.");
        if (request.Items.Count == 0)
            return ServiceResult<DispatchOrderDto>.BadRequest("Koli boş olamaz, en az bir ürün toplayın.");

        var order = await _repository.GetByIdWithDetailsAsync(request.DispatchOrderId);
        if (order is null)
            return ServiceResult<DispatchOrderDto>.NotFound("Dağıtım emri bulunamadı.");
        if (order.CompanyId != request.CompanyId)
            return ServiceResult<DispatchOrderDto>.Forbidden("Bu dağıtım emrine erişim yetkiniz yok.");
        if (order.Status == DispatchOrderStatus.Completed)
            return ServiceResult<DispatchOrderDto>.BadRequest("Tamamlanmış dağıtım emrine koli eklenemez.");

        var boxItems = new List<DispatchBoxItem>();
        string? firstProductName = null;
        var partialWarnings = new List<string>();

        foreach (var reqItem in request.Items)
        {
            if (string.IsNullOrWhiteSpace(reqItem.ProductBarcode))
                return ServiceResult<DispatchOrderDto>.BadRequest("Ürün barkodu zorunludur.");
            if (reqItem.Quantity <= 0)
                return ServiceResult<DispatchOrderDto>.BadRequest("Miktar sıfırdan büyük olmalıdır.");

            var product = await _productRepository.GetByBarcodeAsync(request.CompanyId, reqItem.ProductBarcode);
            if (product is null)
                return ServiceResult<DispatchOrderDto>.NotFound($"Ürün bulunamadı: {reqItem.ProductBarcode}");

            var orderItem = order.Items.FirstOrDefault(i => !i.IsDeleted && i.ProductId == product.Id);
            if (orderItem is null)
                return ServiceResult<DispatchOrderDto>.BadRequest($"{product.Name} bu sipariş listesinde yok.");

            var remaining = orderItem.RequestedQuantity - orderItem.PickedQuantity;
            if (reqItem.Quantity > remaining)
                return ServiceResult<DispatchOrderDto>.BadRequest(
                    $"{product.Name} için istenen {orderItem.RequestedQuantity}, kalan {remaining}, toplanmak istenen {reqItem.Quantity}.");

            // Rafta (OnShelf) olan koliler önce, aynı grup içinde en eski koli (FIFO) önce tüketilir.
            // Tek bir koli yetmezse gerekirse birden fazla koliden bölerek toplanır.
            var candidates = await _boxRepository.GetAvailableForPickingAsync(request.CompanyId, product.Id);
            var totalAvailable = candidates.Sum(b => b.Quantity);
            if (totalAvailable == 0)
                return ServiceResult<DispatchOrderDto>.BadRequest($"{product.Name} için stokta hiç koli bulunamadı.");

            var stillNeeded = Math.Min(reqItem.Quantity, totalAvailable);
            if (stillNeeded < reqItem.Quantity)
                partialWarnings.Add($"{product.Name}: istenen {reqItem.Quantity}, stokta yalnızca {totalAvailable} bulundu, {stillNeeded} toplandı.");

            foreach (var sourceBox in candidates)
            {
                if (stillNeeded == 0)
                    break;

                var takeQty = Math.Min(stillNeeded, sourceBox.Quantity);

                // Koli bir rafa atanmışsa, hangi konumdan toplandığını kayıt altına al.
                var location = await _locationRepository.GetByCurrentBoxIdAsync(sourceBox.Id);

                sourceBox.Quantity -= takeQty;
                if (sourceBox.Quantity == 0)
                {
                    sourceBox.Status = BoxStatus.Dispatched;
                    // Koli tükendi: rafta duruyorsa rafı otomatik boşalt (yoksa "dolu" görünen boş raf kalır).
                    if (location is not null)
                    {
                        location.IsOccupied = false;
                        location.CurrentBoxId = null;
                        await _locationRepository.UpdateAsync(location);
                    }
                }
                await _boxRepository.UpdateAsync(sourceBox);

                orderItem.PickedQuantity += takeQty;
                stillNeeded -= takeQty;

                boxItems.Add(new DispatchBoxItem
                {
                    CompanyId = request.CompanyId,
                    ProductId = product.Id,
                    SourceBoxId = sourceBox.Id,
                    Quantity = takeQty,
                    PickedFromLocationBarcode = location?.Barcode
                });

                firstProductName ??= product.Name;
            }
        }

        string? boxBarcode = null;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var candidate = GenerateBoxBarcode(request.CreatedBy, firstProductName ?? "KOL");
            if (await _repository.GetBoxByBarcodeAsync(request.CompanyId, candidate) is null)
            {
                boxBarcode = candidate;
                break;
            }
        }
        if (boxBarcode is null)
            return ServiceResult<DispatchOrderDto>.BadRequest("Bu kullanıcı ve ürün için eşsiz bir koli barkodu üretilemedi, lütfen tekrar deneyin.");

        var box = new DispatchBox
        {
            CompanyId = request.CompanyId,
            DispatchOrderId = order.Id,
            Barcode = boxBarcode,
            CreatedBy = request.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            Items = boxItems
        };

        await _repository.AddBoxAsync(box);
        await _repository.SaveChangesAsync();

        var updated = await _repository.GetByIdWithDetailsAsync(order.Id);
        var message = $"Sevkiyat kolisi oluşturuldu. Koli Barkodu: {boxBarcode}";
        if (partialWarnings.Count > 0)
            message += " UYARI: " + string.Join(" ", partialWarnings);

        var resultDto = updated is null ? ToDto(order) : ToDto(updated);
        await AttachPickSuggestionsAsync(resultDto);
        return ServiceResult<DispatchOrderDto>.Ok(resultDto, message);
    }

    public async Task<ServiceResult<DispatchOrderDto>> CompleteAsync(CompleteDispatchOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<DispatchOrderDto>.BadRequest("CompanyId zorunludur.");

        var order = await _repository.GetByIdWithDetailsAsync(request.Id);
        if (order is null)
            return ServiceResult<DispatchOrderDto>.NotFound("Dağıtım emri bulunamadı.");
        if (order.CompanyId != request.CompanyId)
            return ServiceResult<DispatchOrderDto>.Forbidden("Bu dağıtım emrine erişim yetkiniz yok.");
        if (order.Status == DispatchOrderStatus.Completed)
            return ServiceResult<DispatchOrderDto>.BadRequest("Dağıtım emri zaten tamamlanmış.");

        var activeItems = order.Items.Where(i => !i.IsDeleted).ToList();
        if (activeItems.Count == 0)
            return ServiceResult<DispatchOrderDto>.BadRequest("Dağıtım emrinde hiç kalem yok.");

        var isFullyPicked = activeItems.All(i => i.PickedQuantity >= i.RequestedQuantity);
        if (!isFullyPicked && !request.ForcePartial)
            return ServiceResult<DispatchOrderDto>.BadRequest(
                "Tüm kalemler tam olarak toplanmadan dağıtım emri tamamlanamaz. Stok yetersizse 'kısmi tamamla' seçeneğini kullanın.");

        order.Status = isFullyPicked ? DispatchOrderStatus.Completed : DispatchOrderStatus.PartiallyCompleted;
        await _repository.UpdateAsync(order);
        await _repository.SaveChangesAsync();

        var updated = await _repository.GetByIdWithDetailsAsync(order.Id);
        var message = isFullyPicked
            ? "Dağıtım emri tamamlandı."
            : "Dağıtım emri stok yetersizliği nedeniyle kısmi olarak tamamlandı. Eksik kalemler için yeni bir sipariş/emir açılması gerekir.";
        return ServiceResult<DispatchOrderDto>.Ok(ToDto(updated!), message);
    }

    public async Task<ServiceResult<DispatchPalletDto>> CreatePalletAsync(CreateDispatchPalletRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<DispatchPalletDto>.BadRequest("CompanyId zorunludur.");
        if (string.IsNullOrWhiteSpace(request.CreatedBy))
            return ServiceResult<DispatchPalletDto>.BadRequest("Kullanıcı adı zorunludur.");
        if (request.BoxBarcodes.Count == 0)
            return ServiceResult<DispatchPalletDto>.BadRequest("En az bir sevkiyat kolisi barkodu okutmalısınız.");

        // Bir palete yalnızca AYNI mağazaya ait koliler eklenebilir — farklı mağazaların kolileri
        // karışmasın diye ilk kolinin mağazası referans alınır, sonraki her koli buna uymalıdır.
        var palletBoxes = new List<DispatchPalletBox>();
        string? palletStoreId = null;
        foreach (var barcode in request.BoxBarcodes)
        {
            var box = await _repository.GetBoxByBarcodeAsync(request.CompanyId, barcode);
            if (box is null)
                return ServiceResult<DispatchPalletDto>.NotFound($"Bu barkoda sahip sevkiyat kolisi bulunamadı: {barcode}");

            var existing = await _palletRepository.GetActivePalletBoxAsync(box.Id);
            if (existing is not null)
                return ServiceResult<DispatchPalletDto>.BadRequest($"{barcode} zaten bir palette.");

            var boxStoreId = box.DispatchOrder?.StoreId ?? string.Empty;
            if (palletStoreId is null)
                palletStoreId = boxStoreId;
            else if (palletStoreId != boxStoreId)
                return ServiceResult<DispatchPalletDto>.BadRequest(
                    $"{barcode} farklı bir mağazaya ait. Bir palete yalnızca aynı mağazanın kolileri eklenebilir.");

            palletBoxes.Add(new DispatchPalletBox
            {
                CompanyId = request.CompanyId,
                DispatchBoxId = box.Id
            });
        }

        string? palletBarcode = null;
        for (var attempt = 0; attempt < 20; attempt++)
        {
            var candidate = GeneratePalletBarcode(request.CreatedBy);
            if (await _palletRepository.GetByBarcodeAsync(request.CompanyId, candidate) is null)
            {
                palletBarcode = candidate;
                break;
            }
        }
        if (palletBarcode is null)
            return ServiceResult<DispatchPalletDto>.BadRequest("Eşsiz bir palet barkodu üretilemedi, lütfen tekrar deneyin.");

        var pallet = new DispatchPallet
        {
            CompanyId = request.CompanyId,
            Barcode = palletBarcode,
            Status = DispatchPalletStatus.Preparing,
            CreatedBy = request.CreatedBy,
            CreatedAt = DateTime.UtcNow,
            PalletBoxes = palletBoxes
        };

        await _palletRepository.AddAsync(pallet);
        await _palletRepository.SaveChangesAsync();

        var created = await _palletRepository.GetByBarcodeAsync(request.CompanyId, palletBarcode);
        return ServiceResult<DispatchPalletDto>.Ok(ToPalletDto(created!),
            $"Palet oluşturuldu (Hazırlanıyor). Koli eklemeye/çıkarmaya devam edebilir, hazır olduğunda sevkiyata onaylayabilirsiniz. Palet Barkodu: {palletBarcode}");
    }

    public async Task<ServiceResult<DispatchPalletDto>> AddBoxToPalletAsync(AddBoxToPalletRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<DispatchPalletDto>.BadRequest("CompanyId zorunludur.");
        if (string.IsNullOrWhiteSpace(request.BoxBarcode))
            return ServiceResult<DispatchPalletDto>.BadRequest("Koli barkodu zorunludur.");

        var pallet = await _palletRepository.GetByIdAsync(request.PalletId);
        if (pallet is null)
            return ServiceResult<DispatchPalletDto>.NotFound("Palet bulunamadı.");
        if (pallet.CompanyId != request.CompanyId)
            return ServiceResult<DispatchPalletDto>.Forbidden("Bu palete erişim yetkiniz yok.");
        if (pallet.Status != DispatchPalletStatus.Preparing)
            return ServiceResult<DispatchPalletDto>.BadRequest("Yalnızca hazırlanma aşamasındaki paletlere koli eklenebilir.");

        var box = await _repository.GetBoxByBarcodeAsync(request.CompanyId, request.BoxBarcode);
        if (box is null)
            return ServiceResult<DispatchPalletDto>.NotFound($"Bu barkoda sahip sevkiyat kolisi bulunamadı: {request.BoxBarcode}");

        var existing = await _palletRepository.GetActivePalletBoxAsync(box.Id);
        if (existing is not null)
            return ServiceResult<DispatchPalletDto>.BadRequest($"{request.BoxBarcode} zaten bir palette.");

        var existingStoreId = pallet.PalletBoxes.Where(pb => !pb.IsDeleted)
            .Select(pb => pb.DispatchBox?.DispatchOrder?.StoreId).FirstOrDefault();
        var newBoxStoreId = box.DispatchOrder?.StoreId;
        if (existingStoreId is not null && existingStoreId != newBoxStoreId)
            return ServiceResult<DispatchPalletDto>.BadRequest(
                $"{request.BoxBarcode} farklı bir mağazaya ait. Bir palete yalnızca aynı mağazanın kolileri eklenebilir.");

        await _palletRepository.AddPalletBoxAsync(new DispatchPalletBox
        {
            CompanyId = request.CompanyId,
            DispatchPalletId = pallet.Id,
            DispatchBoxId = box.Id
        });
        await _palletRepository.SaveChangesAsync();

        var updated = await _palletRepository.GetByIdAsync(pallet.Id);
        return ServiceResult<DispatchPalletDto>.Ok(ToPalletDto(updated!), "Koli palete eklendi.");
    }

    public async Task<ServiceResult<DispatchPalletDto>> RemoveBoxFromPalletAsync(RemoveBoxFromPalletRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<DispatchPalletDto>.BadRequest("CompanyId zorunludur.");

        var pallet = await _palletRepository.GetByIdAsync(request.PalletId);
        if (pallet is null)
            return ServiceResult<DispatchPalletDto>.NotFound("Palet bulunamadı.");
        if (pallet.CompanyId != request.CompanyId)
            return ServiceResult<DispatchPalletDto>.Forbidden("Bu palete erişim yetkiniz yok.");
        if (pallet.Status != DispatchPalletStatus.Preparing)
            return ServiceResult<DispatchPalletDto>.BadRequest("Yalnızca hazırlanma aşamasındaki paletlerden koli çıkarılabilir.");

        var palletBox = pallet.PalletBoxes.FirstOrDefault(pb => !pb.IsDeleted && pb.DispatchBox?.Barcode == request.BoxBarcode);
        if (palletBox is null)
            return ServiceResult<DispatchPalletDto>.NotFound("Bu koli bu palette bulunamadı.");

        await _palletRepository.RemovePalletBoxAsync(palletBox);
        await _palletRepository.SaveChangesAsync();

        var updated = await _palletRepository.GetByIdAsync(pallet.Id);
        return ServiceResult<DispatchPalletDto>.Ok(ToPalletDto(updated!), "Koli paletten çıkarıldı.");
    }

    public async Task<ServiceResult<DispatchPalletDto>> MarkPalletReadyAsync(PalletActionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<DispatchPalletDto>.BadRequest("CompanyId zorunludur.");

        var pallet = await _palletRepository.GetByIdAsync(request.PalletId);
        if (pallet is null)
            return ServiceResult<DispatchPalletDto>.NotFound("Palet bulunamadı.");
        if (pallet.CompanyId != request.CompanyId)
            return ServiceResult<DispatchPalletDto>.Forbidden("Bu palete erişim yetkiniz yok.");
        if (pallet.Status != DispatchPalletStatus.Preparing)
            return ServiceResult<DispatchPalletDto>.BadRequest("Palet zaten sevkiyata hazır ya da sevk edilmiş.");
        if (!pallet.PalletBoxes.Any(pb => !pb.IsDeleted))
            return ServiceResult<DispatchPalletDto>.BadRequest("Boş bir palet sevkiyata hazır olarak onaylanamaz.");

        pallet.Status = DispatchPalletStatus.Ready;
        await _palletRepository.UpdateAsync(pallet);
        await _palletRepository.SaveChangesAsync();

        var updated = await _palletRepository.GetByIdAsync(pallet.Id);
        return ServiceResult<DispatchPalletDto>.Ok(ToPalletDto(updated!), "Palet sevkiyata hazır olarak onaylandı.");
    }

    public async Task<ServiceResult<DispatchPalletDto>> MarkPalletShippedAsync(PalletActionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<DispatchPalletDto>.BadRequest("CompanyId zorunludur.");

        var pallet = await _palletRepository.GetByIdAsync(request.PalletId);
        if (pallet is null)
            return ServiceResult<DispatchPalletDto>.NotFound("Palet bulunamadı.");
        if (pallet.CompanyId != request.CompanyId)
            return ServiceResult<DispatchPalletDto>.Forbidden("Bu palete erişim yetkiniz yok.");
        if (pallet.Status != DispatchPalletStatus.Ready)
            return ServiceResult<DispatchPalletDto>.BadRequest("Yalnızca sevkiyata hazır paletler sevk edildi olarak işaretlenebilir.");

        pallet.Status = DispatchPalletStatus.Shipped;
        await _palletRepository.UpdateAsync(pallet);
        await _palletRepository.SaveChangesAsync();

        var updated = await _palletRepository.GetByIdAsync(pallet.Id);
        return ServiceResult<DispatchPalletDto>.Ok(ToPalletDto(updated!), "Palet sevk edildi olarak işaretlendi.");
    }

    public async Task<ServiceResult<PagedResponse<DispatchPalletDto>>> GetPalletsPagedAsync(DispatchPalletPagedRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<PagedResponse<DispatchPalletDto>>.BadRequest("CompanyId zorunludur.");

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 25 : request.PageSize;

        var (items, totalCount) = await _palletRepository.GetPagedAsync(
            request.CompanyId, page, pageSize, request.Search, request.FromDate, request.ToDate);

        return ServiceResult<PagedResponse<DispatchPalletDto>>.Ok(new PagedResponse<DispatchPalletDto>
        {
            Data = items.Select(ToPalletDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    public async Task<ServiceResult<List<UnpalletizedBoxDto>>> GetUnpalletizedBoxesAsync(string? companyId)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return ServiceResult<List<UnpalletizedBoxDto>>.BadRequest("CompanyId zorunludur.");

        var boxes = await _palletRepository.GetUnpalletizedBoxesAsync(companyId);

        var dtos = boxes.Select(b =>
        {
            var activeItems = b.Items.Where(i => !i.IsDeleted).ToList();
            return new UnpalletizedBoxDto
            {
                Id = b.Id,
                Barcode = b.Barcode,
                DispatchOrderId = b.DispatchOrderId,
                StoreId = b.DispatchOrder?.StoreId ?? string.Empty,
                StoreName = b.DispatchOrder?.StoreName ?? string.Empty,
                CreatedAt = b.CreatedAt,
                ItemQuantity = activeItems.Sum(i => i.Quantity),
                ItemsSummary = string.Join(", ", activeItems.Select(i => $"{i.Product?.Name} x{i.Quantity}"))
            };
        }).ToList();

        return ServiceResult<List<UnpalletizedBoxDto>>.Ok(dtos);
    }

    public async Task<ServiceResult<DispatchPalletDto>> GetPalletByBarcodeAsync(string? companyId, string? barcode)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return ServiceResult<DispatchPalletDto>.BadRequest("CompanyId zorunludur.");
        if (string.IsNullOrWhiteSpace(barcode))
            return ServiceResult<DispatchPalletDto>.BadRequest("Palet barkodu zorunludur.");

        var pallet = await _palletRepository.GetByBarcodeAsync(companyId, barcode);
        if (pallet is null)
            return ServiceResult<DispatchPalletDto>.NotFound("Bu barkoda sahip palet bulunamadı.");

        return ServiceResult<DispatchPalletDto>.Ok(ToPalletDto(pallet));
    }

    private static DispatchPalletDto ToPalletDto(DispatchPallet p)
    {
        var activeBoxes = p.PalletBoxes.Where(pb => !pb.IsDeleted && pb.DispatchBox is not null).ToList();
        var first = activeBoxes.FirstOrDefault();

        return new DispatchPalletDto
        {
            Id = p.Id,
            CompanyId = p.CompanyId,
            Barcode = p.Barcode,
            Status = p.Status,
            StoreId = first?.DispatchBox?.DispatchOrder?.StoreId ?? string.Empty,
            StoreName = first?.DispatchBox?.DispatchOrder?.StoreName ?? string.Empty,
            CreatedBy = p.CreatedBy,
            CreatedAt = p.CreatedAt,
            BoxCount = activeBoxes.Count,
            TotalItemQuantity = activeBoxes.Sum(pb => pb.DispatchBox!.Items.Where(i => !i.IsDeleted).Sum(i => i.Quantity)),
            BoxBarcodes = activeBoxes.Select(pb => pb.DispatchBox!.Barcode).ToList()
        };
    }
}
