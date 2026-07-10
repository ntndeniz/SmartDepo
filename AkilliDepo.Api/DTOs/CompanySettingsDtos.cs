namespace AkilliDepo.Api.DTOs;

public class CompanySettingsDto
{
    public int CorridorCount { get; set; }
    public int ZonesPerCorridor { get; set; }
    public int ShelvesPerZone { get; set; }
    public bool IsConfigured { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UpdateCompanySettingsRequest
{
    public string? CompanyId { get; set; }
    public int CorridorCount { get; set; }
    public int ZonesPerCorridor { get; set; }
    public int ShelvesPerZone { get; set; }
}

public class CompanySettingsRequest
{
    public string? CompanyId { get; set; }
}
