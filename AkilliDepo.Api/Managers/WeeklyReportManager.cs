using System.Text;
using AkilliDepo.Api.DTOs;
using AkilliDepo.Api.Entities;
using AkilliDepo.Api.Repositories;

namespace AkilliDepo.Api.Managers;

public interface IWeeklyReportManager
{
    Task<ServiceResult<PagedResponse<WeeklyReportDto>>> GetPagedAsync(ReportPagedRequest request);
    Task<ServiceResult<(string FileName, byte[] Content)>> DownloadAsync(int id, string? companyId);
    /// <summary>Şirket için, son üretilen rapordan bugüne kadarki (henüz raporlanmamış) haftaları üretir.</summary>
    Task GenerateMissingWeeksAsync(string companyId, DateTime asOf);
}

public class WeeklyReportManager : IWeeklyReportManager
{
    private readonly IWeeklyReportRepository _repository;
    private readonly IReportDataRepository _dataRepository;

    public WeeklyReportManager(IWeeklyReportRepository repository, IReportDataRepository dataRepository)
    {
        _repository = repository;
        _dataRepository = dataRepository;
    }

    private static WeeklyReportDto ToDto(WeeklyReport r) => new()
    {
        Id = r.Id,
        ReportType = r.ReportType,
        WeekStart = r.WeekStart,
        WeekEnd = r.WeekEnd,
        FileName = r.FileName,
        RowCount = r.RowCount,
        GeneratedAt = r.GeneratedAt
    };

    public async Task<ServiceResult<PagedResponse<WeeklyReportDto>>> GetPagedAsync(ReportPagedRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<PagedResponse<WeeklyReportDto>>.BadRequest("CompanyId zorunludur.");

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 25 : request.PageSize;

        var (items, totalCount) = await _repository.GetPagedAsync(
            request.CompanyId, page, pageSize, request.ReportType, request.FromDate, request.ToDate);

        return ServiceResult<PagedResponse<WeeklyReportDto>>.Ok(new PagedResponse<WeeklyReportDto>
        {
            Data = items.Select(ToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    public async Task<ServiceResult<(string FileName, byte[] Content)>> DownloadAsync(int id, string? companyId)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return ServiceResult<(string, byte[])>.BadRequest("CompanyId zorunludur.");

        var report = await _repository.GetByIdAsync(id);
        if (report is null)
            return ServiceResult<(string, byte[])>.NotFound("Rapor bulunamadı.");
        if (report.CompanyId != companyId)
            return ServiceResult<(string, byte[])>.Forbidden("Bu rapora erişim yetkiniz yok.");

        return ServiceResult<(string, byte[])>.Ok((report.FileName, report.Content));
    }

    /// <summary>Pazartesi 00:00 (UTC) haftanın başlangıcı kabul edilir; hafta [WeekStart, WeekEnd) yarı açık aralıktır.</summary>
    private static DateTime StartOfIsoWeek(DateTime date)
    {
        var day = (int)date.DayOfWeek;
        var diff = day == 0 ? 6 : day - 1; // Pazartesi = 0
        return date.Date.AddDays(-diff);
    }

    public async Task GenerateMissingWeeksAsync(string companyId, DateTime asOf)
    {
        var currentWeekStart = StartOfIsoWeek(asOf);

        foreach (var reportType in new[] { WeeklyReportType.GoodsReceipts, WeeklyReportType.Dispatches })
        {
            var lastWeekEnd = await _repository.GetLastWeekEndAsync(companyId, reportType);
            // İlk çalıştırmada geçmişe dönük tek seferde onlarca hafta üretmemek için en fazla 8 hafta geriye bak.
            var weekStart = lastWeekEnd ?? currentWeekStart.AddDays(-7 * 8);

            while (weekStart < currentWeekStart)
            {
                var weekEnd = weekStart.AddDays(7);
                await GenerateOneAsync(companyId, reportType, weekStart, weekEnd);
                weekStart = weekEnd;
            }
        }
    }

    private async Task GenerateOneAsync(string companyId, string reportType, DateTime weekStart, DateTime weekEnd)
    {
        byte[] content;
        int rowCount;
        string fileNameLabel;

        if (reportType == WeeklyReportType.GoodsReceipts)
        {
            var rows = await _dataRepository.GetGoodsReceiptRowsAsync(companyId, weekStart, weekEnd);
            rowCount = rows.Count;
            fileNameLabel = "mal-kabul";

            var sb = new StringBuilder();
            sb.AppendLine("MalKabulId,KabulTarihi,Marka,Urun,Barkod,KoliBarkodu,SayilanMiktar,OlusturanKullanici");
            foreach (var r in rows)
            {
                sb.AppendLine(string.Join(",", Csv(r.GoodsReceiptId.ToString()), Csv(r.ReceivedAt.ToString("yyyy-MM-dd HH:mm")),
                    Csv(r.BrandName), Csv(r.ProductName), Csv(r.ProductBarcode), Csv(r.BoxBarcode),
                    Csv(r.CountedQuantity.ToString()), Csv(r.ReceiptCreatedBy)));
            }
            content = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }
        else
        {
            var rows = await _dataRepository.GetDispatchRowsAsync(companyId, weekStart, weekEnd);
            rowCount = rows.Count;
            fileNameLabel = "sevkiyat";

            var sb = new StringBuilder();
            sb.AppendLine("DagitimEmriId,OlusturmaTarihi,Durum,MagazaId,MagazaAdi,Urun,Renk,IstenenMiktar,ToplananMiktar,OlusturanKullanici");
            foreach (var r in rows)
            {
                sb.AppendLine(string.Join(",", Csv(r.DispatchOrderId.ToString()), Csv(r.DispatchCreatedAt.ToString("yyyy-MM-dd HH:mm")),
                    Csv(r.Status), Csv(r.StoreId), Csv(r.StoreName), Csv(r.ProductName), Csv(r.Color),
                    Csv(r.RequestedQuantity.ToString()), Csv(r.PickedQuantity.ToString()), Csv(r.CreatedBy)));
            }
            content = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
        }

        var report = new WeeklyReport
        {
            CompanyId = companyId,
            ReportType = reportType,
            WeekStart = weekStart,
            WeekEnd = weekEnd,
            FileName = $"{fileNameLabel}-{weekStart:yyyy-MM-dd}_{weekEnd.AddDays(-1):yyyy-MM-dd}.csv",
            Content = content,
            RowCount = rowCount,
            GeneratedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(report);
        await _repository.SaveChangesAsync();
    }

    private static string Csv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
