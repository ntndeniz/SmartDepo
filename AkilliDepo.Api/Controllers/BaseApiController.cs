using AkilliDepo.Api.DTOs;
using AkilliDepo.Api.Managers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AkilliDepo.Api.Controllers;

[ApiController]
[Authorize]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// JWT'den doğrulanmış CompanyId. Client'ın gönderdiği CompanyId asla güvenilmez;
    /// her action bu değeri request'e yazarak client değerinin üzerine yazmalıdır.
    /// </summary>
    protected string CurrentCompanyId => User.FindFirst("CompanyId")?.Value ?? string.Empty;

    /// <summary>JWT'den doğrulanmış kullanıcı adı — "CreatedBy" gibi hesap verebilirlik alanları için kullanılır.</summary>
    protected string CurrentUsername => User.Identity?.Name ?? string.Empty;

    /// <summary>JWT'den doğrulanmış rol (Admin/Staff).</summary>
    protected string CurrentUserRole => User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value ?? string.Empty;

    /// <summary>JWT'den doğrulanmış kullanıcı id'si.</summary>
    protected int CurrentUserId =>
        int.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

    protected IActionResult FromResult<T>(ServiceResult<T> result)
    {
        return result.Status switch
        {
            ServiceStatus.Ok => Ok(ApiResponse<T>.Ok(result.Data!, result.Message)),
            ServiceStatus.BadRequest => BadRequest(ApiResponse<T>.Fail(result.Message ?? "Geçersiz istek.")),
            ServiceStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden,
                ApiResponse<T>.Fail(result.Message ?? "Bu kayda erişim yetkiniz yok.")),
            ServiceStatus.NotFound => NotFound(ApiResponse<T>.Fail(result.Message ?? "Kayıt bulunamadı.")),
            _ => StatusCode(500, ApiResponse<T>.Fail("Beklenmeyen hata."))
        };
    }
}
