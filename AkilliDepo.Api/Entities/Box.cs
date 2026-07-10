namespace AkilliDepo.Api.Entities;

public static class BoxStatus
{
    public const string InStock = "InStock";
    public const string OnShelf = "OnShelf";
    public const string Dispatched = "Dispatched";
}

public class Box : BaseEntity
{
    public string Barcode { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal? Desi { get; set; }
    public string Status { get; set; } = BoxStatus.InStock;
    public string CreatedBy { get; set; } = string.Empty;
    public string ProductColor { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Product? Product { get; set; }
}
