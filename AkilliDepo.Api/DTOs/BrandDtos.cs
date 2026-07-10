namespace AkilliDepo.Api.DTOs;

public class BrandDto
{
    public int Id { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ShortCode { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateBrandRequest
{
    public string? CompanyId { get; set; }
    public string? Name { get; set; }
}

public class UpdateBrandRequest
{
    public int Id { get; set; }
    public string? CompanyId { get; set; }
    public string? Name { get; set; }
}
