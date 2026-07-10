namespace AkilliDepo.Api.Entities;

public class GoodsReceiptItem : BaseEntity
{
    public int GoodsReceiptId { get; set; }
    public int ProductId { get; set; }
    public int BrandId { get; set; }
    public int BoxId { get; set; }
    public int CountedQuantity { get; set; }
    public DateTime CreatedAt { get; set; }

    public GoodsReceipt? GoodsReceipt { get; set; }
    public Product? Product { get; set; }
    public Brand? Brand { get; set; }
    public Box? Box { get; set; }
}
