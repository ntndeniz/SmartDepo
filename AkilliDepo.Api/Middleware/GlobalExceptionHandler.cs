using AkilliDepo.Api.DTOs;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace AkilliDepo.Api.Middleware;

/// <summary>
/// Yakalanmamış tüm hataları merkezi olarak yakalar; ham stack trace/istisna metnini asla
/// istemciye döndürmez, yalnızca loglar. Benzersizlik (unique index) çakışmaları 409 olarak,
/// diğer her şey 500 olarak yapılandırılmış bir ApiResponse ile döner.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Yakalanmamış hata: {Path}", httpContext.Request.Path);

        var isUniqueViolation = exception is DbUpdateException dbEx
            && dbEx.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true;

        httpContext.Response.ContentType = "application/json";
        httpContext.Response.StatusCode = isUniqueViolation ? StatusCodes.Status409Conflict : StatusCodes.Status500InternalServerError;

        var message = isUniqueViolation
            ? "Kayıt çakışması oluştu (ör. barkod eşsizliği), lütfen tekrar deneyin."
            : "Beklenmeyen bir sunucu hatası oluştu.";

        await httpContext.Response.WriteAsJsonAsync(ApiResponse<object>.Fail(message), cancellationToken);
        return true;
    }
}
