using AkilliDepo.Api.Data;
using AkilliDepo.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkilliDepo.Api.Repositories;

/// <summary>Bir StoreOrder'a karşılık gelen DispatchOrder'ın (varsa) özet durumu.</summary>
public record DispatchStatusLookup(int StoreOrderId, int DispatchOrderId, string Status);

public interface IDispatchRepository
{
    Task<(List<DispatchOrder> Items, int TotalCount)> GetPagedAsync(
        string companyId, int page, int pageSize, string? search, string? status, DateTime? fromDate, DateTime? toDate);
    Task<DispatchOrder?> GetByIdAsync(int id);
    Task<DispatchOrder?> GetByIdWithDetailsAsync(int id);
    /// <summary>Bu mağaza siparişi için zaten bir dağıtım emri açıldı mı — aynı sipariş barkodu birden
    /// fazla kez okutulduğunda (çift tarama, çift tıklama) yinelenen dağıtım emri oluşmasını önlemek için.</summary>
    Task<DispatchOrder?> GetByStoreOrderIdAsync(string companyId, int storeOrderId);
    Task<DispatchBox?> GetBoxByBarcodeAsync(string companyId, string barcode);
    Task<List<DispatchStatusLookup>> GetStatusesByStoreOrderIdsAsync(string companyId, List<int> storeOrderIds);
    Task AddAsync(DispatchOrder order);
    Task AddBoxAsync(DispatchBox box);
    Task UpdateAsync(DispatchOrder order);
    Task SaveChangesAsync();
}

public class DispatchRepository : IDispatchRepository
{
    private readonly AppDbContext _context;

    public DispatchRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<DispatchOrder> WithDetails() =>
        _context.DispatchOrders
            .Include(o => o.StoreOrder)
            .Include(o => o.Items)
            .ThenInclude(i => i.Product)
            .Include(o => o.Boxes)
            .ThenInclude(b => b.Items)
            .ThenInclude(i => i.Product)
            .Include(o => o.Boxes)
            .ThenInclude(b => b.Items)
            .ThenInclude(i => i.SourceBox);

    public async Task<(List<DispatchOrder> Items, int TotalCount)> GetPagedAsync(
        string companyId, int page, int pageSize, string? search, string? status, DateTime? fromDate, DateTime? toDate)
    {
        var query = WithDetails().AsNoTracking().Where(o => o.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(o => o.StoreName.Contains(search) || o.StoreId.Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(o => o.Status == status);
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

    public Task<DispatchOrder?> GetByIdAsync(int id) =>
        _context.DispatchOrders.FirstOrDefaultAsync(o => o.Id == id);

    public Task<DispatchOrder?> GetByIdWithDetailsAsync(int id) =>
        WithDetails().FirstOrDefaultAsync(o => o.Id == id);

    public Task<DispatchOrder?> GetByStoreOrderIdAsync(string companyId, int storeOrderId) =>
        WithDetails().FirstOrDefaultAsync(o => o.CompanyId == companyId && o.StoreOrderId == storeOrderId);

    public async Task<List<DispatchStatusLookup>> GetStatusesByStoreOrderIdsAsync(string companyId, List<int> storeOrderIds) =>
        await _context.DispatchOrders.AsNoTracking()
            .Where(o => o.CompanyId == companyId && storeOrderIds.Contains(o.StoreOrderId))
            .Select(o => new DispatchStatusLookup(o.StoreOrderId, o.Id, o.Status))
            .ToListAsync();

    // NOT: .Include(b => b.DispatchOrder) olmadan box.DispatchOrder her zaman null dönerdi; bu da
    // palet oluşturma/koli ekleme sırasındaki "aynı mağaza" doğrulamasını (StoreId karşılaştırması
    // "" != "" olduğu için) sessizce her zaman geçerli kılıyordu — canlı testte doğrulanan bug.
    public Task<DispatchBox?> GetBoxByBarcodeAsync(string companyId, string barcode) =>
        _context.DispatchBoxes
            .Include(b => b.DispatchOrder)
            .FirstOrDefaultAsync(b => b.CompanyId == companyId && b.Barcode == barcode);

    public async Task AddAsync(DispatchOrder order)
    {
        await _context.DispatchOrders.AddAsync(order);
    }

    public async Task AddBoxAsync(DispatchBox box)
    {
        await _context.DispatchBoxes.AddAsync(box);
    }

    public Task UpdateAsync(DispatchOrder order)
    {
        _context.Entry(order).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
