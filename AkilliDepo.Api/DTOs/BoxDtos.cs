namespace AkilliDepo.Api.DTOs;

public class BoxDto
{
    public int Id { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string ProductBarcode { get; set; } = string.Empty;
    public string ProductColor { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal? Desi { get; set; }
    public string Status { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateBoxRequest
{
    public string? CompanyId { get; set; }
    public string? CreatedBy { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal? Desi { get; set; }
}

public class UpdateBoxRequest
{
    public int Id { get; set; }
    public string? CompanyId { get; set; }
    public int Quantity { get; set; }
    public decimal? Desi { get; set; }
    /// <summary>Miktar değiştiriliyorsa zorunlu: sayım farkı/fire/hasar gibi bir gerekçe.</summary>
    public string? Reason { get; set; }
    public string? AdjustedBy { get; set; }
}

public class StockAdjustmentDto
{
    public int Id { get; set; }
    public int BoxId { get; set; }
    public string BoxBarcode { get; set; } = string.Empty;
    public int OldQuantity { get; set; }
    public int NewQuantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string AdjustedBy { get; set; } = string.Empty;
    public DateTime AdjustedAt { get; set; }
}

public class BoxPagedRequest : PagedRequest
{
    public string? Status { get; set; }
}
