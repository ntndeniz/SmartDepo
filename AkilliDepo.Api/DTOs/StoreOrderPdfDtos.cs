namespace AkilliDepo.Api.DTOs;

public class ParsedOrderItemDto
{
    public int? ProductId { get; set; }
    public string ProductBarcode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int Quantity { get; set; }
    /// <summary>Barkod sistemde tanımlı bir ürünle eşleşti mi. false ise bu satır siparişe eklenemez.</summary>
    public bool Matched { get; set; }
}

public class ParsedStoreOrderDto
{
    public string? StoreId { get; set; }
    public string? StoreName { get; set; }
    public string? Address { get; set; }
    public List<ParsedOrderItemDto> Items { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
