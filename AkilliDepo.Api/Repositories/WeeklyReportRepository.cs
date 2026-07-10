using AkilliDepo.Api.Data;
using AkilliDepo.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkilliDepo.Api.Repositories;

public interface IWeeklyReportRepository
{
    Task<(List<WeeklyReport> Items, int TotalCount)> GetPagedAsync(
        string companyId, int page, int pageSize, string? reportType, DateTime? fromDate, DateTime? toDate);
    Task<WeeklyReport?> GetByIdAsync(int id);
    Task<DateTime?> GetLastWeekEndAsync(string companyId, string reportType);
    Task AddAsync(WeeklyReport report);
    Task SaveChangesAsync();
}

public class WeeklyReportRepository : IWeeklyReportRepository
{
    private readonly AppDbContext _context;

    public WeeklyReportRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(List<WeeklyReport> Items, int TotalCount)> GetPagedAsync(
        string companyId, int page, int pageSize, string? reportType, DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.WeeklyReports.AsNoTracking()
            .Where(r => r.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(reportType))
        {
            query = query.Where(r => r.ReportType == reportType);
        }

        // Rapor haftalık bir aralığı (WeekStart..WeekEnd) temsil ettiği için "tarih filtresi" o
        // haftanın seçilen aralıkla KESİŞTİĞİ raporları döner (tek bir günle eşleşmesi beklenemez).
        if (fromDate.HasValue)
        {
            query = query.Where(r => r.WeekEnd >= fromDate.Value.Date);
        }
        if (toDate.HasValue)
        {
            query = query.Where(r => r.WeekStart < toDate.Value.Date.AddDays(1));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.WeekStart)
            .Select(r => new WeeklyReport
            {
                Id = r.Id,
                CompanyId = r.CompanyId,
                ReportType = r.ReportType,
                WeekStart = r.WeekStart,
                WeekEnd = r.WeekEnd,
                FileName = r.FileName,
                RowCount = r.RowCount,
                GeneratedAt = r.GeneratedAt
                // Content kasıtlı olarak dahil edilmedi: liste ekranı büyük blob'ları çekmemeli.
            })
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<WeeklyReport?> GetByIdAsync(int id) =>
        _context.WeeklyReports.FirstOrDefaultAsync(r => r.Id == id);

    public Task<DateTime?> GetLastWeekEndAsync(string companyId, string reportType) =>
        _context.WeeklyReports
            .Where(r => r.CompanyId == companyId && r.ReportType == reportType)
            .OrderByDescending(r => r.WeekEnd)
            .Select(r => (DateTime?)r.WeekEnd)
            .FirstOrDefaultAsync();

    public async Task AddAsync(WeeklyReport report)
    {
        await _context.WeeklyReports.AddAsync(report);
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
