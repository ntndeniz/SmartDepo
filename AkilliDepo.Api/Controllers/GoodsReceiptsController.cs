using AkilliDepo.Api.DTOs;
using AkilliDepo.Api.Managers;
using Microsoft.AspNetCore.Mvc;

namespace AkilliDepo.Api.Controllers;

[Route("api/goods-receipts")]
public class GoodsReceiptsController : BaseApiController
{
    private readonly IGoodsReceiptManager _manager;

    public GoodsReceiptsController(IGoodsReceiptManager manager)
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

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        return FromResult(await _manager.GetByIdAsync(id, CurrentCompanyId));
    }

    [HttpGet("items")]
    public async Task<IActionResult> ListItems([FromQuery] PagedRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        var result = await _manager.GetItemsPagedAsync(request);
        if (!result.IsSuccess)
            return FromResult(result);
        return Ok(result.Data);
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateGoodsReceiptSessionRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        return FromResult(await _manager.CreateSessionAsync(request));
    }

    [HttpPost("scan-item")]
    public async Task<IActionResult> ScanItem([FromBody] ScanGoodsReceiptItemRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        request.CreatedBy = CurrentUsername;
        return FromResult(await _manager.ScanItemAsync(request));
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody] DeleteRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        return FromResult(await _manager.DeleteAsync(request));
    }
}
