namespace AkilliDepo.Api.Entities;

public static class WeeklyReportType
{
    public const string GoodsReceipts = "GoodsReceipts";
    public const string Dispatches = "Dispatches";
}

/// <summary>
/// Arka planda haftalık olarak üretilip saklanan CSV rapor kaydı. İçerik veritabanında tutulur,
/// böylece dosya sistemi/depolama yönetimi gerekmez ve DB yedeğiyle birlikte korunur.
/// </summary>
public class WeeklyReport : BaseEntity
{
    public string ReportType { get; set; } = string.Empty;
    public DateTime WeekStart { get; set; }
    public DateTime WeekEnd { get; set; }
    public string FileName { get; set; } = string.Empty;
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public int RowCount { get; set; }
    public DateTime GeneratedAt { get; set; }
}
