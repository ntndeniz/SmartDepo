using AkilliDepo.Api.DTOs;
using AkilliDepo.Api.Managers;
using Microsoft.AspNetCore.Mvc;

namespace AkilliDepo.Api.Controllers;

[Route("api/store-orders")]
public class StoreOrdersController : BaseApiController
{
    private readonly IStoreOrderManager _manager;

    public StoreOrdersController(IStoreOrderManager manager)
    {
        _manager = manager;
    }

    [HttpGet("list")]
    public async Task<IActionResult> List([FromQuery] PagedRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        var result = await _manager.GetPagedAsync(request);
        if (!result.IsSuccess)
            return FromResult(result);
        return Ok(result.Data);
    }

    [HttpGet("by-code")]
    public async Task<IActionResult> GetByCode([FromQuery] string? orderCode)
    {
        return FromResult(await _manager.GetByCodeAsync(CurrentCompanyId, orderCode));
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateStoreOrderRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        return FromResult(await _manager.CreateAsync(request));
    }

    [HttpPost("parse-pdf")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> ParsePdf(IFormFile? file)
    {
        if (file is null || file.Length == 0)
            return FromResult(ServiceResult<ParsedStoreOrderDto>.BadRequest("PDF dosyası zorunludur."));
        if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
            return FromResult(ServiceResult<ParsedStoreOrderDto>.BadRequest("Yalnızca .pdf dosyası yüklenebilir."));

        await using var stream = file.OpenReadStream();
        return FromResult(await _manager.ParsePdfAsync(CurrentCompanyId, stream));
    }
}
