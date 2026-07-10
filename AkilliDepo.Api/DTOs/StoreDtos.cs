namespace AkilliDepo.Api.DTOs;

public class StoreDto
{
    public int Id { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public string StoreCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateStoreRequest
{
    public string? CompanyId { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
}

public class UpdateStoreRequest
{
    public int Id { get; set; }
    public string? CompanyId { get; set; }
    public string? Name { get; set; }
    public string? Address { get; set; }
}
