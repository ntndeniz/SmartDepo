namespace AkilliDepo.Api.Entities;

/// <summary>
/// BoxManager.UpdateAsync ile bir kolinin miktarı elle değiştirildiğinde oluşan denetim (audit) kaydı.
/// Sayım farkı, fire, hasar gibi sebepsiz stok değişikliklerinin iz bırakmadan yapılmasını engeller.
/// </summary>
public class StockAdjustment : BaseEntity
{
    public int BoxId { get; set; }
    public int OldQuantity { get; set; }
    public int NewQuantity { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string AdjustedBy { get; set; } = string.Empty;
    public DateTime AdjustedAt { get; set; }

    public Box? Box { get; set; }
}
