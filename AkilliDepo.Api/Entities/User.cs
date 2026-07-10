namespace AkilliDepo.Api.Entities;

public static class UserRole
{
    /// <summary>Kullanıcı yönetimi, marka/ürün silme, firma ayarları gibi geri dönüşü zor işlemleri yapabilir.</summary>
    public const string Admin = "Admin";
    /// <summary>Günlük operasyon: mal kabul, toplama, koli/konum işlemleri. Kullanıcı yönetimi ve silme işlemlerine erişemez.</summary>
    public const string Staff = "Staff";
}

public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PasswordSalt { get; set; } = string.Empty;
    public string Role { get; set; } = UserRole.Staff;
    public DateTime CreatedAt { get; set; }
}
