namespace AkilliDepo.Api.DTOs;

public class ProductDto
{
    public int Id { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int BrandId { get; set; }
    public string BrandName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateProductRequest
{
    public string? CompanyId { get; set; }
    public string? Name { get; set; }
    public string? Color { get; set; }
    public string? Unit { get; set; }
    public int BrandId { get; set; }
}

public class UpdateProductRequest
{
    public int Id { get; set; }
    public string? CompanyId { get; set; }
    public string? Name { get; set; }
    public string? Color { get; set; }
    public string? Unit { get; set; }
    public int BrandId { get; set; }
}

public class ProductPagedRequest : PagedRequest
{
    public int? BrandId { get; set; }
}

public class BulkProductRowRequest
{
    public string? Name { get; set; }
    public string? Color { get; set; }
    public string? Unit { get; set; }
}

public class BulkCreateProductsRequest
{
    public string? CompanyId { get; set; }
    public int BrandId { get; set; }
    public List<BulkProductRowRequest> Items { get; set; } = new();
}

public class BulkCreateRowResultDto
{
    public int RowNumber { get; set; }
    public string? Name { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? Barcode { get; set; }
}

public class BulkCreateProductsResultDto
{
    public int CreatedCount { get; set; }
    public List<BulkCreateRowResultDto> Rows { get; set; } = new();
}
