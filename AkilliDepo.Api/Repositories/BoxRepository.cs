using AkilliDepo.Api.Data;
using AkilliDepo.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkilliDepo.Api.Repositories;

public record PickSuggestion(string BoxBarcode, int AvailableQuantity, string Status, string? LocationBarcode);

public interface IBoxRepository
{
    Task<(List<Box> Items, int TotalCount)> GetPagedAsync(
        string companyId, int page, int pageSize, string? search, string? status);
    Task<Box?> GetByIdAsync(int id);
    Task<Box?> GetByBarcodeAsync(string companyId, string barcode);
    Task<bool> AnyByProductAsync(string companyId, int productId);
    /// <summary>Bu koli daha önce bir sevkiyat kolisine kaynak olarak kullanıldı mı (DispatchBoxItem.SourceBoxId).
    /// Sonucu evet ise koli soft-delete edilemez — SourceBoxId non-nullable FK olduğu için silinirse
    /// geçmiş sevkiyat kayıtları Include (INNER JOIN) sorgusundan sessizce düşer.</summary>
    Task<bool> IsReferencedAsDispatchSourceAsync(int boxId);
    /// <summary>
    /// Bir üründen toplama yapılabilecek tüm kolileri, önce rafta olanlar (OnShelf) sonra henüz
    /// yerleşmemiş olanlar (InStock) sırasıyla, her grup içinde en eski (FIFO) önce gelecek şekilde döner.
    /// Toplama mantığı bu sırayı kullanarak gerekirse birden fazla koliden bölerek toplar.
    /// </summary>
    Task<List<Box>> GetAvailableForPickingAsync(string companyId, int productId);
    /// <summary>Aynı sırayı (rafta önce, sonra FIFO) konum bilgisiyle birlikte döner — "bu ürünü nerede bulurum" ekranı için.</summary>
    Task<List<PickSuggestion>> GetPickSuggestionsAsync(string companyId, int productId);
    Task AddAsync(Box box);
    Task UpdateAsync(Box box);
    Task SaveChangesAsync();
}

public class BoxRepository : IBoxRepository
{
    private readonly AppDbContext _context;

    public BoxRepository(AppDbContext context)
    {
        _context = context;
    }

    // NOT: Box.ProductId non-nullable olduğu için EF Core .Include(b => b.Product) çağrısını INNER JOIN'e
    // çevirir; Product soft-delete edilmişse (IsDeleted=1) o Product'ı taşıyan koli INNER JOIN'den tamamen
    // düşer — fiziksel stok hâlâ dururken sistemde "kaybolur". Bunu önlemek için Product tarafında
    // IgnoreQueryFilters() ile elle LEFT JOIN kuruyoruz: koli her zaman görünür, ürünü silinmişse bile.
    // ÖNEMLİ: Bu bileşik (join + DefaultIfEmpty) sorguda Products.IgnoreQueryFilters() çağrısı, EF
    // Core'un sorgu ağacını derleme şeklinden dolayı Boxes tarafının KENDİ IsDeleted global filtresini
    // de (istenmeden) devre dışı bırakıyor — bu yüzden "!b.IsDeleted" burada AÇIKÇA eklenmelidir, global
    // filtreye güvenilemez (canlı SQL loglarıyla doğrulandı: filtre olmadan silinen koliler listede
    // kalıyor ve rafları kalıcı olarak "dolu/orphan" bırakıyordu).
    public async Task<(List<Box> Items, int TotalCount)> GetPagedAsync(
        string companyId, int page, int pageSize, string? search, string? status)
    {
        var query =
            from b in _context.Boxes.AsNoTracking()
            where b.CompanyId == companyId && !b.IsDeleted
            join p in _context.Products.IgnoreQueryFilters().AsNoTracking() on b.ProductId equals p.Id into pj
            from p in pj.DefaultIfEmpty()
            select new { Box = b, Product = p };

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(x =>
                x.Box.Barcode.Contains(search) ||
                (x.Product != null && x.Product.Name.Contains(search)));
        }

        // "Active" özel bir gerçek durum değildir — frontend'in varsayılan filtresi için "sevk
        // edilmemiş her şey" (InStock + OnShelf) anlamına gelir.
        if (status == "Active")
        {
            query = query.Where(x => x.Box.Status != BoxStatus.Dispatched);
        }
        else if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Box.Status == status);
        }

        var totalCount = await query.CountAsync();
        var rows = await query
            .OrderByDescending(x => x.Box.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = rows.Select(x =>
        {
            x.Box.Product = x.Product;
            return x.Box;
        }).ToList();

        return (items, totalCount);
    }

    public async Task<Box?> GetByIdAsync(int id)
    {
        var row = await (
            from b in _context.Boxes
            where b.Id == id && !b.IsDeleted
            join p in _context.Products.IgnoreQueryFilters() on b.ProductId equals p.Id into pj
            from p in pj.DefaultIfEmpty()
            select new { Box = b, Product = p }).FirstOrDefaultAsync();

        if (row is null) return null;
        row.Box.Product = row.Product;
        return row.Box;
    }

    public Task<Box?> GetByBarcodeAsync(string companyId, string barcode) =>
        _context.Boxes.FirstOrDefaultAsync(b => b.CompanyId == companyId && b.Barcode == barcode);

    public Task<bool> AnyByProductAsync(string companyId, int productId) =>
        _context.Boxes.AnyAsync(b => b.CompanyId == companyId && b.ProductId == productId);

    public Task<bool> IsReferencedAsDispatchSourceAsync(int boxId) =>
        _context.DispatchBoxItems.AnyAsync(i => i.SourceBoxId == boxId);

    public async Task<List<Box>> GetAvailableForPickingAsync(string companyId, int productId) =>
        await _context.Boxes
            .Where(b => b.CompanyId == companyId && b.ProductId == productId
                && b.Status != BoxStatus.Dispatched && b.Quantity > 0)
            .OrderByDescending(b => b.Status == BoxStatus.OnShelf)
            .ThenBy(b => b.Id)
            .ToListAsync();

    // Yalnızca rafa yerleştirilmiş (OnShelf) koliler döner: henüz rafa konmamış koliler burada
    // "seçenek" olarak gösterilmez (kafa karıştırır) — toplama sırasında gerekirse yine kullanılır,
    // ama bu "nerede bulunur" ipucu ekranında yalnızca fiilen gidip alınabilecek raflar listelenir.
    // Sıralama depoya giriş tarihine (CreatedAt) göredir: en eski koli önce toplanmalıdır (FIFO).
    public async Task<List<PickSuggestion>> GetPickSuggestionsAsync(string companyId, int productId)
    {
        var shelfQuery =
            from b in _context.Boxes
            where b.CompanyId == companyId && b.ProductId == productId
                && b.Status == BoxStatus.OnShelf && b.Quantity > 0
            join l in _context.Locations on b.Id equals l.CurrentBoxId into lj
            from l in lj.DefaultIfEmpty()
            orderby b.CreatedAt
            select new { b.Barcode, b.Quantity, b.Status, LocationBarcode = (string?)l.Barcode };

        var rows = await shelfQuery.ToListAsync();
        if (rows.Count > 0)
            return rows.Select(r => new PickSuggestion(r.Barcode, r.Quantity, r.Status, r.LocationBarcode)).ToList();

        // Hiçbir koli rafa yerleştirilmemişse, kullanıcıya "bulunamıyor" demek yerine en azından hangi
        // koli barkodunu fiziksel olarak arayacağını söylüyoruz (henüz rafa taşınmamış, ör. mal kabul
        // alanında olabilir) — LocationBarcode null kalır, frontend bunu ayrı bir uyarıyla gösterir.
        var unshelvedQuery =
            from b in _context.Boxes
            where b.CompanyId == companyId && b.ProductId == productId
                && b.Status == BoxStatus.InStock && b.Quantity > 0
            orderby b.CreatedAt
            select new { b.Barcode, b.Quantity, b.Status };

        var unshelved = await unshelvedQuery.ToListAsync();
        return unshelved.Select(r => new PickSuggestion(r.Barcode, r.Quantity, r.Status, null)).ToList();
    }

    public async Task AddAsync(Box box)
    {
        await _context.Boxes.AddAsync(box);
    }

    public Task UpdateAsync(Box box)
    {
        _context.Entry(box).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}
