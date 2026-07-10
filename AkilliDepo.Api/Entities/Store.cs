namespace AkilliDepo.Api.Entities;

/// <summary>
/// Sipariş veren mağaza kalıcı kaydı. Aynı mağaza farklı tarihlerde sipariş verdiğinde isme göre
/// eşleştirilip aynı StoreCode'un kullanılması sağlanır (bkz. StoreManager.GetOrCreateAsync) —
/// önceden StoreId her seferinde serbest metin olarak giriliyordu, bu da aynı mağaza için farklı
/// ID'lerin oluşmasına yol açabiliyordu.
/// </summary>
public class Store : BaseEntity
{
    /// <summary>Brand.ShortCode ile aynı desende (3 karakter) üretilir.</summary>
    public string StoreCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
