using AkilliDepo.Api.Data;
using AkilliDepo.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkilliDepo.Api.Repositories;

public interface IGoodsReceiptRepository
{
    Task<(List<GoodsReceipt> Items, int TotalCount)> GetPagedAsync(
        string companyId, int page, int pageSize, string? search);
    Task<GoodsReceipt?> GetByIdAsync(int id);
    Task<GoodsReceipt?> GetByIdWithItemsAsync(int id);
    Task<(List<GoodsReceiptItem> Items, int TotalCount)> GetItemsPagedAsync(
        string companyId, int page, int pageSize, string? search, DateTime? fromDate, DateTime? toDate);
    Task AddAsync(GoodsReceipt receipt);
    Task AddItemAsync(GoodsReceiptItem item);
    Task UpdateAsync(GoodsReceipt receipt);
    Task SaveChangesAsync();
}

public class GoodsReceiptRepository : IGoodsReceiptRepository
{
    private readonly AppDbContext _context;

    public GoodsReceiptRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(List<GoodsReceipt> Items, int TotalCount)> GetPagedAsync(
        string companyId, int page, int pageSize, string? search)
    {
        var query = _context.GoodsReceipts.AsNoTracking()
            .Include(r => r.Items)
            .ThenInclude(i => i.Product)
            .Include(r => r.Items)
            .ThenInclude(i => i.Brand)
            .Include(r => r.Items)
            .ThenInclude(i => i.Box)
            .Where(r => r.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(r => r.Items.Any(i =>
                i.Product!.Name.Contains(search) || i.Box!.Barcode.Contains(search)));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(r => r.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public async Task<(List<GoodsReceiptItem> Items, int TotalCount)> GetItemsPagedAsync(
        string companyId, int page, int pageSize, string? search, DateTime? fromDate, DateTime? toDate)
    {
        var query = _context.GoodsReceiptItems.AsNoTracking()
            .Include(i => i.Product)
            .Include(i => i.Brand)
            .Include(i => i.Box)
            .Where(i => i.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(i =>
                (i.Product != null && (i.Product.Name.Contains(search) || i.Product.Barcode.Contains(search) || i.Product.Color.Contains(search))) ||
                (i.Brand != null && i.Brand.Name.Contains(search)));
        }

        if (fromDate.HasValue)
        {
            query = query.Where(i => i.CreatedAt >= fromDate.Value.Date);
        }
        if (toDate.HasValue)
        {
            query = query.Where(i => i.CreatedAt < toDate.Value.Date.AddDays(1));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<GoodsReceipt?> GetByIdWithItemsAsync(int id) =>
        _context.GoodsReceipts
            .Include(r => r.Items)
            .ThenInclude(i => i.Product)
            .Include(r => r.Items)
            .ThenInclude(i => i.Brand)
            .Include(r => r.Items)
            .ThenInclude(i => i.Box)
            .FirstOrDefaultAsync(r => r.Id == id);

    public Task<GoodsReceipt?> GetByIdAsync(int id) =>
        _context.GoodsReceipts.FirstOrDefaultAsync(r => r.Id == id);

    public async Task AddAsync(GoodsReceipt receipt)
    {
        await _context.GoodsReceipts.AddAsync(receipt);
    }

    public async Task AddItemAsync(GoodsReceiptItem item)
    {
        await _context.GoodsReceiptItems.AddAsync(item);
    }

    public Task UpdateAsync(GoodsReceipt receipt)
    {
        _context.Entry(receipt).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
