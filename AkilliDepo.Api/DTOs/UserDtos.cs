namespace AkilliDepo.Api.DTOs;

public class UserDto
{
    public int Id { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class LoginRequest
{
    public string? CompanyId { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
}

public class CreateUserRequest
{
    public string? CompanyId { get; set; }
    public string? Username { get; set; }
    public string? Password { get; set; }
    /// <summary>Admin/Staff. Belirtilmezse Staff olarak oluşturulur.</summary>
    public string? Role { get; set; }
}

public class LoginResultDto
{
    public string Token { get; set; } = string.Empty;
    public UserDto User { get; set; } = new();
}
