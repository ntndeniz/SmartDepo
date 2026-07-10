namespace AkilliDepo.Api.Entities;

public class DispatchOrderItem : BaseEntity
{
    public int DispatchOrderId { get; set; }
    public int ProductId { get; set; }
    public string Color { get; set; } = string.Empty;
    public int RequestedQuantity { get; set; }
    public int PickedQuantity { get; set; }

    public DispatchOrder? DispatchOrder { get; set; }
    public Product? Product { get; set; }
}
