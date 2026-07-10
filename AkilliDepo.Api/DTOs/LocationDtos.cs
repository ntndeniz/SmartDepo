namespace AkilliDepo.Api.DTOs;

public class LocationDto
{
    public int Id { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public int CorridorNo { get; set; }
    public int ZoneNo { get; set; }
    public int ShelfNo { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public bool IsOccupied { get; set; }
    public int? CurrentBoxId { get; set; }
    public string? CurrentBoxBarcode { get; set; }
    public string? CurrentBoxProductName { get; set; }
}

public class GenerateLocationsRequest
{
    public string? CompanyId { get; set; }
}

public class GenerateLocationsResultDto
{
    public int CreatedCount { get; set; }
    public int TotalCount { get; set; }
}

public class AssignBoxRequest
{
    public string? CompanyId { get; set; }
    public int LocationId { get; set; }
    public string? BoxBarcode { get; set; }
}

public class ReleaseLocationRequest
{
    public string? CompanyId { get; set; }
    public int LocationId { get; set; }
}

public class LocationPagedRequest : PagedRequest
{
    public bool? IsOccupied { get; set; }
}
