using AkilliDepo.Api.Data;
using AkilliDepo.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkilliDepo.Api.Repositories;

public interface IBrandRepository
{
    Task<(List<Brand> Items, int TotalCount)> GetPagedAsync(
        string companyId, int page, int pageSize, string? search);
    Task<Brand?> GetByIdAsync(int id);
    Task<Brand?> GetByNameAsync(string companyId, string name);
    Task<Brand?> GetByShortCodeAsync(string companyId, string shortCode);
    Task AddAsync(Brand brand);
    Task UpdateAsync(Brand brand);
    Task SaveChangesAsync();
}

public class BrandRepository : IBrandRepository
{
    private readonly AppDbContext _context;

    public BrandRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(List<Brand> Items, int TotalCount)> GetPagedAsync(
        string companyId, int page, int pageSize, string? search)
    {
        var query = _context.Brands.AsNoTracking()
            .Where(b => b.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(b => b.Name.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(b => b.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<Brand?> GetByIdAsync(int id) =>
        _context.Brands.FirstOrDefaultAsync(b => b.Id == id);

    public Task<Brand?> GetByNameAsync(string companyId, string name) =>
        _context.Brands.FirstOrDefaultAsync(b => b.CompanyId == companyId && b.Name == name);

    public Task<Brand?> GetByShortCodeAsync(string companyId, string shortCode) =>
        _context.Brands.FirstOrDefaultAsync(b => b.CompanyId == companyId && b.ShortCode == shortCode);

    public async Task AddAsync(Brand brand)
    {
        await _context.Brands.AddAsync(brand);
    }

    public Task UpdateAsync(Brand brand)
    {
        _context.Entry(brand).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
