using AkilliDepo.Api.Data;
using AkilliDepo.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkilliDepo.Api.Repositories;

public interface IStockAdjustmentRepository
{
    Task<(List<StockAdjustment> Items, int TotalCount)> GetPagedAsync(
        string companyId, int page, int pageSize, int? boxId);
    Task AddAsync(StockAdjustment adjustment);
    Task SaveChangesAsync();
}

public class StockAdjustmentRepository : IStockAdjustmentRepository
{
    private readonly AppDbContext _context;

    public StockAdjustmentRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(List<StockAdjustment> Items, int TotalCount)> GetPagedAsync(
        string companyId, int page, int pageSize, int? boxId)
    {
        var query = _context.StockAdjustments.AsNoTracking()
            .Include(a => a.Box)
            .Where(a => a.CompanyId == companyId);

        if (boxId.HasValue)
        {
            query = query.Where(a => a.BoxId == boxId.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(a => a.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task AddAsync(StockAdjustment adjustment)
    {
        await _context.StockAdjustments.AddAsync(adjustment);
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
