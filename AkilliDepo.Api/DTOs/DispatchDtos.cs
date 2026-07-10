namespace AkilliDepo.Api.DTOs;

public class DispatchOrderItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductBarcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int RequestedQuantity { get; set; }
    public int PickedQuantity { get; set; }
    /// <summary>
    /// Bu ürünü depoda nerede bulabileceğinizi gösterir: hangi koli, ne kadar stok, hangi rafta
    /// (rafa henüz konmamışsa null). Toplama sırasında sistemin gerçekte tüketeceği sırayla
    /// (önce rafta olanlar, sonra FIFO) sıralanır.
    /// </summary>
    public List<PickSuggestionDto> Suggestions { get; set; } = new();
}

public class PickSuggestionDto
{
    public string BoxBarcode { get; set; } = string.Empty;
    public int AvailableQuantity { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? LocationBarcode { get; set; }
}

public class DispatchBoxItemDto
{
    public int Id { get; set; }
    public int SourceBoxId { get; set; }
    public string SourceBoxBarcode { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? PickedFromLocationBarcode { get; set; }
}

public class DispatchBoxDto
{
    public int Id { get; set; }
    public int DispatchOrderId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<DispatchBoxItemDto> Items { get; set; } = new();
}

public class DispatchOrderDto
{
    public int Id { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public int StoreOrderId { get; set; }
    public string StoreOrderCode { get; set; } = string.Empty;
    public string StoreId { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<DispatchOrderItemDto> Items { get; set; } = new();
    public List<DispatchBoxDto> Boxes { get; set; } = new();
}

public class CreateFromStoreOrderRequest
{
    public string? CompanyId { get; set; }
    public string? StoreOrderCode { get; set; }
    public string? CreatedBy { get; set; }
}

public class CloseBoxItemRequest
{
    public string? ProductBarcode { get; set; }
    public int Quantity { get; set; }
}

public class CloseDispatchBoxRequest
{
    public string? CompanyId { get; set; }
    public int DispatchOrderId { get; set; }
    public string? CreatedBy { get; set; }
    public List<CloseBoxItemRequest> Items { get; set; } = new();
}

public class CompleteDispatchOrderRequest
{
    public string? CompanyId { get; set; }
    public int Id { get; set; }
    /// <summary>Stok yetersizliği nedeniyle bazı kalemler tam toplanamadıysa, yine de emri kısmi olarak kapatmak için true gönderilir.</summary>
    public bool ForcePartial { get; set; }
}

public class DispatchPagedRequest : PagedRequest
{
    public string? Status { get; set; }
}

public class CreateDispatchPalletRequest
{
    public string? CompanyId { get; set; }
    public string? CreatedBy { get; set; }
    /// <summary>Koli sayısı sınırsızdır (kullanıcı karar verir), ama hepsi AYNI mağazaya ait olmalıdır.</summary>
    public List<string> BoxBarcodes { get; set; } = new();
}

public class DispatchPalletDto
{
    public int Id { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StoreId { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int BoxCount { get; set; }
    public int TotalItemQuantity { get; set; }
    public List<string> BoxBarcodes { get; set; } = new();
}

public class AddBoxToPalletRequest
{
    public string? CompanyId { get; set; }
    public int PalletId { get; set; }
    public string? BoxBarcode { get; set; }
}

public class RemoveBoxFromPalletRequest
{
    public string? CompanyId { get; set; }
    public int PalletId { get; set; }
    public string? BoxBarcode { get; set; }
}

public class PalletActionRequest
{
    public string? CompanyId { get; set; }
    public int PalletId { get; set; }
}

public class UnpalletizedBoxDto
{
    public int Id { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public int DispatchOrderId { get; set; }
    public string StoreId { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int ItemQuantity { get; set; }
    public string ItemsSummary { get; set; } = string.Empty;
}

public class DispatchPalletPagedRequest : PagedRequest
{
    /// <summary>Palet oluşturulma tarihi bu tarihten (dahil, gün başlangıcı) sonra olmalı.</summary>
    public DateTime? FromDate { get; set; }
    /// <summary>Palet oluşturulma tarihi bu tarihten (dahil, gün sonu) önce olmalı.</summary>
    public DateTime? ToDate { get; set; }
}
