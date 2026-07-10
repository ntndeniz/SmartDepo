namespace AkilliDepo.Api.Entities;

public static class DispatchOrderStatus
{
    public const string Picking = "Picking";
    public const string Completed = "Completed";
    /// <summary>Stok yetersizliği nedeniyle tüm kalemler tam toplanamadan bilinçli olarak kapatıldı.</summary>
    public const string PartiallyCompleted = "PartiallyCompleted";
}

public class DispatchOrder : BaseEntity
{
    public int StoreOrderId { get; set; }
    public string StoreId { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Status { get; set; } = DispatchOrderStatus.Picking;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public StoreOrder? StoreOrder { get; set; }
    public ICollection<DispatchOrderItem> Items { get; set; } = new List<DispatchOrderItem>();
    public ICollection<DispatchBox> Boxes { get; set; } = new List<DispatchBox>();
}
