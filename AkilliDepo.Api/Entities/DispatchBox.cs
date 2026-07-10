namespace AkilliDepo.Api.Entities;

public class DispatchBox : BaseEntity
{
    public int DispatchOrderId { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public DispatchOrder? DispatchOrder { get; set; }
    public ICollection<DispatchBoxItem> Items { get; set; } = new List<DispatchBoxItem>();
}
