namespace AkilliDepo.Api.DTOs;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message) =>
        new() { Success = false, Message = message };
}

public class PagedResponse<T>
{
    public bool Success { get; set; } = true;
    public List<T> Data { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}

public class PagedRequest
{
    public string? CompanyId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string? Search { get; set; }
    /// <summary>Opsiyonel tarih aralığı filtresi — yalnızca bunu destekleyen repository'lerde
    /// (Mal Kabul, Siparişler, Sevkiyat) uygulanır, diğerlerinde yoksayılır.</summary>
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

public class DeleteRequest
{
    public int Id { get; set; }
    public string? CompanyId { get; set; }
}
