namespace AkilliDepo.Api.Entities;

/// <summary>
/// Şirket bazlı genel ayarlar. Şu an yalnızca depo boyutu (koridor/bölge/raf sayıları) tutuluyor;
/// ileride başka firma ayarları eklenmek istenirse buraya yeni alan eklemek yeterli — şirket başına
/// tek satır olduğundan ayrı bir anahtar-değer tablosuna gerek yok.
/// </summary>
public class CompanySettings : BaseEntity
{
    public int CorridorCount { get; set; }
    public int ZonesPerCorridor { get; set; }
    public int ShelvesPerZone { get; set; }
    public DateTime UpdatedAt { get; set; }
}
