using AkilliDepo.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace AkilliDepo.Api.Repositories;

public record GoodsReceiptReportRow(
    int GoodsReceiptId, DateTime ReceivedAt, string ReceiptCreatedBy,
    string BrandName, string ProductName, string ProductBarcode,
    string BoxBarcode, int CountedQuantity);

public record DispatchReportRow(
    int DispatchOrderId, DateTime DispatchCreatedAt, string Status,
    string StoreId, string StoreName, string ProductName, string Color,
    int RequestedQuantity, int PickedQuantity, string CreatedBy);

/// <summary>
/// Haftalık rapor CSV'lerini üretmek için ham veri çeken salt-okunur sorgular. Descriptive (açıklayıcı)
/// tablolar (Product/Brand/Box/StoreOrder) elle LEFT JOIN + IgnoreQueryFilters ile bağlanır: aksi halde
/// EF Core Include'un otomatik INNER JOIN'i, soft-delete edilmiş bir ürün/marka yüzünden geçmiş rapor
/// satırlarını sessizce düşürebilir (bkz. Box↔Product bug'ı, CALISMA_RAPORU.md).
/// </summary>
public interface IReportDataRepository
{
    Task<List<GoodsReceiptReportRow>> GetGoodsReceiptRowsAsync(string companyId, DateTime fromDate, DateTime toDate);
    Task<List<DispatchReportRow>> GetDispatchRowsAsync(string companyId, DateTime fromDate, DateTime toDate);
}

public class ReportDataRepository : IReportDataRepository
{
    private readonly AppDbContext _context;

    public ReportDataRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<GoodsReceiptReportRow>> GetGoodsReceiptRowsAsync(string companyId, DateTime fromDate, DateTime toDate)
    {
        var query =
            from i in _context.GoodsReceiptItems
            join r in _context.GoodsReceipts on i.GoodsReceiptId equals r.Id
            join p in _context.Products.IgnoreQueryFilters() on i.ProductId equals p.Id into pj
            from p in pj.DefaultIfEmpty()
            join b in _context.Brands.IgnoreQueryFilters() on i.BrandId equals b.Id into bj
            from b in bj.DefaultIfEmpty()
            join box in _context.Boxes.IgnoreQueryFilters() on i.BoxId equals box.Id into boxj
            from box in boxj.DefaultIfEmpty()
            where i.CompanyId == companyId && r.ReceivedAt >= fromDate && r.ReceivedAt < toDate
            orderby r.ReceivedAt
            select new GoodsReceiptReportRow(
                r.Id, r.ReceivedAt, r.CreatedBy,
                b != null ? b.Name : "(silinmiş marka)",
                p != null ? p.Name : "(silinmiş ürün)",
                p != null ? p.Barcode : "-",
                box != null ? box.Barcode : "-",
                i.CountedQuantity);

        return await query.ToListAsync();
    }

    public async Task<List<DispatchReportRow>> GetDispatchRowsAsync(string companyId, DateTime fromDate, DateTime toDate)
    {
        var query =
            from i in _context.DispatchOrderItems
            join o in _context.DispatchOrders on i.DispatchOrderId equals o.Id
            join p in _context.Products.IgnoreQueryFilters() on i.ProductId equals p.Id into pj
            from p in pj.DefaultIfEmpty()
            where i.CompanyId == companyId && o.CreatedAt >= fromDate && o.CreatedAt < toDate
            orderby o.CreatedAt
            select new DispatchReportRow(
                o.Id, o.CreatedAt, o.Status,
                o.StoreId, o.StoreName,
                p != null ? p.Name : "(silinmiş ürün)",
                i.Color,
                i.RequestedQuantity, i.PickedQuantity, o.CreatedBy);

        return await query.ToListAsync();
    }
}
