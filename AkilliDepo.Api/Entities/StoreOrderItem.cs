namespace AkilliDepo.Api.Entities;

public class StoreOrderItem : BaseEntity
{
    public int StoreOrderId { get; set; }
    public int ProductId { get; set; }
    public string Color { get; set; } = string.Empty;
    public int Quantity { get; set; }

    public StoreOrder? StoreOrder { get; set; }
    public Product? Product { get; set; }
}
