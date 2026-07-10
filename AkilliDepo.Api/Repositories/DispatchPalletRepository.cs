using AkilliDepo.Api.Data;
using AkilliDepo.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkilliDepo.Api.Repositories;

public interface IDispatchPalletRepository
{
    Task<DispatchPallet?> GetByIdAsync(int id);
    Task<DispatchPallet?> GetByBarcodeAsync(string companyId, string barcode);
    Task<(List<DispatchPallet> Items, int TotalCount)> GetPagedAsync(
        string companyId, int page, int pageSize, string? search, DateTime? fromDate, DateTime? toDate);
    Task<DispatchPalletBox?> GetActivePalletBoxAsync(int dispatchBoxId);
    Task<List<DispatchBox>> GetUnpalletizedBoxesAsync(string companyId);
    Task<Dictionary<int, string>> GetOrderRollupStatusesAsync(string companyId, List<int> dispatchOrderIds);
    Task AddAsync(DispatchPallet pallet);
    Task AddPalletBoxAsync(DispatchPalletBox palletBox);
    Task RemovePalletBoxAsync(DispatchPalletBox palletBox);
    Task UpdateAsync(DispatchPallet pallet);
    Task SaveChangesAsync();
}

public class DispatchPalletRepository : IDispatchPalletRepository
{
    private readonly AppDbContext _context;

    public DispatchPalletRepository(AppDbContext context)
    {
        _context = context;
    }

    private IQueryable<DispatchPallet> WithDetails() =>
        _context.DispatchPallets
            .Include(p => p.PalletBoxes)
            .ThenInclude(pb => pb.DispatchBox)
            .ThenInclude(b => b!.DispatchOrder)
            .Include(p => p.PalletBoxes)
            .ThenInclude(pb => pb.DispatchBox)
            .ThenInclude(b => b!.Items);

    public Task<DispatchPallet?> GetByIdAsync(int id) =>
        WithDetails().FirstOrDefaultAsync(p => p.Id == id);

    public Task<DispatchPallet?> GetByBarcodeAsync(string companyId, string barcode) =>
        WithDetails().FirstOrDefaultAsync(p => p.CompanyId == companyId && p.Barcode == barcode);

    public async Task<(List<DispatchPallet> Items, int TotalCount)> GetPagedAsync(
        string companyId, int page, int pageSize, string? search, DateTime? fromDate, DateTime? toDate)
    {
        var query = WithDetails().AsNoTracking().Where(p => p.CompanyId == companyId);

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => p.Barcode.Contains(search));
        }

        if (fromDate.HasValue)
        {
            query = query.Where(p => p.CreatedAt >= fromDate.Value.Date);
        }
        if (toDate.HasValue)
        {
            query = query.Where(p => p.CreatedAt < toDate.Value.Date.AddDays(1));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    public Task<DispatchPalletBox?> GetActivePalletBoxAsync(int dispatchBoxId) =>
        _context.DispatchPalletBoxes
            .FirstOrDefaultAsync(pb => pb.DispatchBoxId == dispatchBoxId);

    // Henüz hiçbir DispatchPalletBox'ta yer almamış sevkiyat kolileri — en yeni önce, ki yeni
    // kapatılan bir koli "Paletler" ekranının en üstünde hemen görünsün.
    public async Task<List<DispatchBox>> GetUnpalletizedBoxesAsync(string companyId)
    {
        var palletizedBoxIds = _context.DispatchPalletBoxes
            .Where(pb => pb.CompanyId == companyId)
            .Select(pb => pb.DispatchBoxId);

        return await _context.DispatchBoxes
            .AsNoTracking()
            .Include(b => b.DispatchOrder)
            .Include(b => b.Items)
            .ThenInclude(i => i.Product)
            .Where(b => b.CompanyId == companyId && !palletizedBoxIds.Contains(b.Id))
            .OrderByDescending(b => b.Id)
            .ToListAsync();
    }

    /// <summary>
    /// Tamamlanmış (Completed) bir dağıtım emrinin bütün kolileri paletlenmişse, o paletlerin
    /// durumundan yola çıkarak sipariş için "ReadyToShip" (hepsi Ready/Shipped) veya "Shipped"
    /// (hepsi Shipped) yuvarlanmış bir durum üretir. Koliler henüz eksik/kısmi paletlenmişse
    /// (veya hiç paletlenmemişse) o siparişe hiç girdi eklenmez — çağıran taraf "Completed" olarak
    /// göstermeye devam eder.
    /// </summary>
    public async Task<Dictionary<int, string>> GetOrderRollupStatusesAsync(string companyId, List<int> dispatchOrderIds)
    {
        var boxRows = await _context.DispatchBoxes
            .AsNoTracking()
            .Where(b => b.CompanyId == companyId && dispatchOrderIds.Contains(b.DispatchOrderId))
            .Select(b => new
            {
                b.DispatchOrderId,
                PalletStatus = _context.DispatchPalletBoxes
                    .Where(pb => pb.DispatchBoxId == b.Id)
                    .Select(pb => pb.DispatchPallet!.Status)
                    .FirstOrDefault()
            })
            .ToListAsync();

        var result = new Dictionary<int, string>();
        foreach (var group in boxRows.GroupBy(r => r.DispatchOrderId))
        {
            var statuses = group.Select(r => r.PalletStatus).ToList();
            if (statuses.Count == 0 || statuses.Any(string.IsNullOrEmpty))
                continue; // en az bir koli henüz paletlenmemiş

            if (statuses.All(s => s == DispatchPalletStatus.Shipped))
                result[group.Key] = "Shipped";
            else if (statuses.All(s => s == DispatchPalletStatus.Ready || s == DispatchPalletStatus.Shipped))
                result[group.Key] = "ReadyToShip";
        }

        return result;
    }

    public async Task AddAsync(DispatchPallet pallet)
    {
        await _context.DispatchPallets.AddAsync(pallet);
    }

    public async Task AddPalletBoxAsync(DispatchPalletBox palletBox)
    {
        await _context.DispatchPalletBoxes.AddAsync(palletBox);
    }

    public Task RemovePalletBoxAsync(DispatchPalletBox palletBox)
    {
        palletBox.IsDeleted = true;
        _context.Entry(palletBox).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(DispatchPallet pallet)
    {
        _context.Entry(pallet).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
