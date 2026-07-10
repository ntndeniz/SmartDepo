namespace AkilliDepo.Api.Entities;

public class StoreOrder : BaseEntity
{
    public string OrderCode { get; set; } = string.Empty;
    public string StoreId { get; set; } = string.Empty;
    public string StoreName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public ICollection<StoreOrderItem> Items { get; set; } = new List<StoreOrderItem>();
}
