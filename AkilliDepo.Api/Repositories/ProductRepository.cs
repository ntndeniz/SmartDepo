using AkilliDepo.Api.Data;
using AkilliDepo.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkilliDepo.Api.Repositories;

public interface IProductRepository
{
    Task<(List<Product> Items, int TotalCount)> GetPagedAsync(
        string companyId, int page, int pageSize, string? search, int? brandId);
    Task<Product?> GetByIdAsync(int id);
    Task<Product?> GetByBarcodeAsync(string companyId, string barcode);
    Task<bool> AnyByBrandAsync(string companyId, int brandId);
    Task<List<Product>> GetAllActiveAsync(string companyId);
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task SaveChangesAsync();
}

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;

    public ProductRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(List<Product> Items, int TotalCount)> GetPagedAsync(
        string companyId, int page, int pageSize, string? search, int? brandId)
    {
        var query = _context.Products.AsNoTracking()
            .Include(p => p.Brand)
            .Where(p => p.CompanyId == companyId);

        if (brandId.HasValue)
        {
            query = query.Where(p => p.BrandId == brandId.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p =>
                p.Name.Contains(search) ||
                p.Barcode.Contains(search) ||
                p.Color.Contains(search) ||
                p.Unit.Contains(search));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<Product?> GetByIdAsync(int id) =>
        _context.Products.Include(p => p.Brand).FirstOrDefaultAsync(p => p.Id == id);

    public Task<Product?> GetByBarcodeAsync(string companyId, string barcode) =>
        _context.Products.Include(p => p.Brand)
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Barcode == barcode);

    public Task<bool> AnyByBrandAsync(string companyId, int brandId) =>
        _context.Products.AnyAsync(p => p.CompanyId == companyId && p.BrandId == brandId);

    public Task<List<Product>> GetAllActiveAsync(string companyId) =>
        _context.Products.AsNoTracking().Where(p => p.CompanyId == companyId).ToListAsync();

    public async Task AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);
    }

    public Task UpdateAsync(Product product)
    {
        _context.Entry(product).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
