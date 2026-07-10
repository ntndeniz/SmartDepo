namespace AkilliDepo.Api.DTOs;

public class StoreOrderItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductBarcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int Quantity { get; set; }
}

public class StoreOrderDto
{
    public int Id { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public string OrderCode { get; set; } = string.Empty;
    public string StoreId { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public List<StoreOrderItemDto> Items { get; set; } = new();
    /// <summary>Bu siparişe karşılık bir DispatchOrder açıldıysa onun durumu; hiç açılmadıysa null ("Bekliyor").</summary>
    public string? DispatchStatus { get; set; }
    public int? DispatchOrderId { get; set; }
}

public class CreateStoreOrderItemRequest
{
    public int ProductId { get; set; }
    public string? Color { get; set; }
    public int Quantity { get; set; }
}

public class CreateStoreOrderRequest
{
    public string? CompanyId { get; set; }
    public string? StoreName { get; set; }
    public string? Address { get; set; }
    public List<CreateStoreOrderItemRequest> Items { get; set; } = new();
}
