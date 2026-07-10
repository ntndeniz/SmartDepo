namespace AkilliDepo.Api.DTOs;

public class GoodsReceiptItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductBarcode { get; set; } = string.Empty;
    public string ProductColor { get; set; } = string.Empty;
    public int BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public int BoxId { get; set; }
    public string BoxBarcode { get; set; } = string.Empty;
    public decimal? Desi { get; set; }
    public int CountedQuantity { get; set; }
    public int CumulativeQuantity { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class GoodsReceiptDto
{
    public int Id { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public DateTime ReceivedAt { get; set; }
    public List<GoodsReceiptItemDto> Items { get; set; } = new();
}

public class CreateGoodsReceiptSessionRequest
{
    public string? CompanyId { get; set; }
}

public class ScanGoodsReceiptItemRequest
{
    public string? CompanyId { get; set; }
    public int GoodsReceiptId { get; set; }
    public string? ProductBarcode { get; set; }
    public int Quantity { get; set; }
    public decimal? Desi { get; set; }
    public string? CreatedBy { get; set; }
}
