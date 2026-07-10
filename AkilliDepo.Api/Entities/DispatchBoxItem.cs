namespace AkilliDepo.Api.Entities;

public class DispatchBoxItem : BaseEntity
{
    public int DispatchBoxId { get; set; }
    public int SourceBoxId { get; set; }
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    /// <summary>Kaynak koli toplama anında bir rafa atanmışsa o rafın barkodu (hangi konumdan alındığının kaydı).</summary>
    public string? PickedFromLocationBarcode { get; set; }

    public DispatchBox? DispatchBox { get; set; }
    public Box? SourceBox { get; set; }
    public Product? Product { get; set; }
}
