namespace AkilliDepo.Api.Entities;

public static class DispatchPalletStatus
{
    /// <summary>Koli eklenip çıkarılabilir, henüz sevkiyata onaylanmamış.</summary>
    public const string Preparing = "Preparing";
    /// <summary>Kullanıcı sevkiyata onay verdi; koli listesi artık kilitli.</summary>
    public const string Ready = "Ready";
    /// <summary>Fiilen sevk edildi.</summary>
    public const string Shipped = "Shipped";
}

/// <summary>
/// Sevkiyat paleti. Bir palete yalnızca AYNI mağazaya ait koliler eklenebilir (bkz.
/// DispatchManager.CreatePalletAsync/AddBoxToPalletAsync); koli sayısı sınırsızdır, kullanıcı karar verir.
/// Palet "Preparing" durumunda serbestçe düzenlenebilir (koli ekle/çıkar); kullanıcı "sevkiyata hazır"
/// onayı verince "Ready" olur ve koli listesi kilitlenir; fiilen sevk edildiğinde "Shipped" olur.
/// </summary>
public class DispatchPallet : BaseEntity
{
    public string Barcode { get; set; } = string.Empty;
    public string Status { get; set; } = DispatchPalletStatus.Preparing;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public ICollection<DispatchPalletBox> PalletBoxes { get; set; } = new List<DispatchPalletBox>();
}
