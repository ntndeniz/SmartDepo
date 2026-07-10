namespace AkilliDepo.Api.DTOs;

public class WeeklyReportDto
{
    public int Id { get; set; }
    public string ReportType { get; set; } = string.Empty;
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public string FileName { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public DateTime GeneratedAt { get; set; }
}

public class ReportPagedRequest : PagedRequest
{
    public string? ReportType { get; set; }
}

public class GenerateReportNowRequest
{
    public string? CompanyId { get; set; }
}
