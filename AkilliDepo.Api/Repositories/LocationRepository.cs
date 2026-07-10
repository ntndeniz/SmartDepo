using AkilliDepo.Api.Data;
using AkilliDepo.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkilliDepo.Api.Repositories;

public interface ILocationRepository
{
    Task<(List<Location> Items, int TotalCount)> GetPagedAsync(
        string companyId, int page, int pageSize, string? search, bool? isOccupied);
    Task<Location?> GetByIdAsync(int id);
    Task<Location?> GetByCoordinatesAsync(string companyId, int corridorNo, int zoneNo, int shelfNo);
    Task<Location?> GetByCurrentBoxIdAsync(int boxId);
    Task<List<(int CorridorNo, int ZoneNo, int ShelfNo)>> GetExistingCoordinatesAsync(string companyId);
    Task AddAsync(Location location);
    Task UpdateAsync(Location location);
    Task SaveChangesAsync();
}

public class LocationRepository : ILocationRepository
{
    private readonly AppDbContext _context;

    public LocationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<(List<Location> Items, int TotalCount)> GetPagedAsync(
        string companyId, int page, int pageSize, string? search, bool? isOccupied)
    {
        var query = _context.Locations.AsNoTracking()
            .Include(l => l.CurrentBox)
            .ThenInclude(b => b!.Product)
            .Where(l => l.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(l => l.Barcode.Contains(search));
        }

        if (isOccupied.HasValue)
        {
            query = query.Where(l => l.IsOccupied == isOccupied.Value);
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(l => l.CorridorNo)
            .ThenBy(l => l.ZoneNo)
            .ThenBy(l => l.ShelfNo)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<Location?> GetByIdAsync(int id) =>
        _context.Locations
            .Include(l => l.CurrentBox)
            .ThenInclude(b => b!.Product)
            .FirstOrDefaultAsync(l => l.Id == id);

    public Task<Location?> GetByCoordinatesAsync(string companyId, int corridorNo, int zoneNo, int shelfNo) =>
        _context.Locations.FirstOrDefaultAsync(l =>
            l.CompanyId == companyId &&
            l.CorridorNo == corridorNo &&
            l.ZoneNo == zoneNo &&
            l.ShelfNo == shelfNo);

    public Task<Location?> GetByCurrentBoxIdAsync(int boxId) =>
        _context.Locations.FirstOrDefaultAsync(l => l.CurrentBoxId == boxId);

    public async Task<List<(int CorridorNo, int ZoneNo, int ShelfNo)>> GetExistingCoordinatesAsync(string companyId)
    {
        var rows = await _context.Locations.AsNoTracking()
            .Where(l => l.CompanyId == companyId)
            .Select(l => new { l.CorridorNo, l.ZoneNo, l.ShelfNo })
            .ToListAsync();

        return rows.Select(r => (r.CorridorNo, r.ZoneNo, r.ShelfNo)).ToList();
    }

    public async Task AddAsync(Location location)
    {
        await _context.Locations.AddAsync(location);
    }

    public Task UpdateAsync(Location location)
    {
        _context.Entry(location).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
