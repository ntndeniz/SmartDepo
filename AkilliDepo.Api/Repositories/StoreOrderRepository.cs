using AkilliDepo.Api.Data;
using AkilliDepo.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkilliDepo.Api.Repositories;

public interface IStoreOrderRepository
{
    Task<(List<StoreOrder> Items, int TotalCount)> GetPagedAsync(
        string companyId, int page, int pageSize, string? search, DateTime? fromDate, DateTime? toDate);
    Task<StoreOrder?> GetByIdAsync(int id);
    Task<StoreOrder?> GetByCodeAsync(string companyId, string orderCode);
    Task<bool> AnyByStoreCodeAsync(string companyId, string storeCode);
    Task AddAsync(StoreOrder order);
    Task SaveChangesAsync();
}

public class StoreOrderRepository : IStoreOrderRepository
{
    private readonly AppDbContext _context;

    public StoreOrderRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(List<StoreOrder> Items, int TotalCount)> GetPagedAsync(
        string companyId, int page, int pageSize, string? search, DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.StoreOrders.AsNoTracking()
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Where(o => o.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(o =>
                o.OrderCode.Contains(search) ||
                o.StoreName.Contains(search) ||
                o.StoreId.Contains(search));
        }

        if (fromDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt >= fromDate.Value.Date);
        }
        if (toDate.HasValue)
        {
            query = query.Where(o => o.CreatedAt < toDate.Value.Date.AddDays(1));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(o => o.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<StoreOrder?> GetByIdAsync(int id) =>
        _context.StoreOrders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

    public Task<StoreOrder?> GetByCodeAsync(string companyId, string orderCode) =>
        _context.StoreOrders
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.CompanyId == companyId && o.OrderCode == orderCode);

    public Task<bool> AnyByStoreCodeAsync(string companyId, string storeCode) =>
        _context.StoreOrders.AnyAsync(o => o.CompanyId == companyId && o.StoreId == storeCode);

    public async Task AddAsync(StoreOrder order)
    {
        await _context.StoreOrders.AddAsync(order);
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
