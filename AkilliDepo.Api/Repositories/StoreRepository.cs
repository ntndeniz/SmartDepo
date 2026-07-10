using AkilliDepo.Api.Data;
using AkilliDepo.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkilliDepo.Api.Repositories;

public interface IStoreRepository
{
    Task<(List<Store> Items, int TotalCount)> GetPagedAsync(string companyId, int page, int pageSize, string? search);
    Task<Store?> GetByIdAsync(int id);
    Task<Store?> GetByNameAsync(string companyId, string name);
    Task<Store?> GetByStoreCodeAsync(string companyId, string storeCode);
    Task AddAsync(Store store);
    Task UpdateAsync(Store store);
    Task SaveChangesAsync();
}

public class StoreRepository : IStoreRepository
{
    private readonly AppDbContext _context;

    public StoreRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(List<Store> Items, int TotalCount)> GetPagedAsync(string companyId, int page, int pageSize, string? search)
    {
        var query = _context.Stores.AsNoTracking().Where(s => s.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(s => s.Name.Contains(search) || s.StoreCode.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(s => s.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<Store?> GetByIdAsync(int id) =>
        _context.Stores.FirstOrDefaultAsync(s => s.Id == id);

    public Task<Store?> GetByNameAsync(string companyId, string name) =>
        _context.Stores.FirstOrDefaultAsync(s => s.CompanyId == companyId && s.Name == name);

    public Task<Store?> GetByStoreCodeAsync(string companyId, string storeCode) =>
        _context.Stores.FirstOrDefaultAsync(s => s.CompanyId == companyId && s.StoreCode == storeCode);

    public async Task AddAsync(Store store)
    {
        await _context.Stores.AddAsync(store);
    }

    public Task UpdateAsync(Store store)
    {
        _context.Entry(store).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
