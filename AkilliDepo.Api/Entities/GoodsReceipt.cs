namespace AkilliDepo.Api.Entities;

public class GoodsReceipt : BaseEntity
{
    public DateTime ReceivedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;

    public ICollection<GoodsReceiptItem> Items { get; set; } = new List<GoodsReceiptItem>();
}
