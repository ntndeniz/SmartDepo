using AkilliDepo.Api.DTOs;
using AkilliDepo.Api.Managers;
using Microsoft.AspNetCore.Mvc;

namespace AkilliDepo.Api.Controllers;

[Route("api/dispatch-orders")]
public class DispatchOrdersController : BaseApiController
{
    private readonly IDispatchManager _manager;

    public DispatchOrdersController(IDispatchManager manager)
    {
        _manager = manager;
    }

    [HttpGet("list")]
    public async Task<IActionResult> List([FromQuery] DispatchPagedRequest request)
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

    [HttpPost("create-from-store-order")]
    public async Task<IActionResult> CreateFromStoreOrder([FromBody] CreateFromStoreOrderRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        request.CreatedBy = CurrentUsername;
        return FromResult(await _manager.CreateFromStoreOrderAsync(request));
    }

    [HttpPost("close-box")]
    public async Task<IActionResult> CloseBox([FromBody] CloseDispatchBoxRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        request.CreatedBy = CurrentUsername;
        return FromResult(await _manager.CloseBoxAsync(request));
    }

    [HttpPost("complete")]
    public async Task<IActionResult> Complete([FromBody] CompleteDispatchOrderRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        return FromResult(await _manager.CompleteAsync(request));
    }

    [HttpPost("create-pallet")]
    public async Task<IActionResult> CreatePallet([FromBody] CreateDispatchPalletRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        request.CreatedBy = CurrentUsername;
        return FromResult(await _manager.CreatePalletAsync(request));
    }

    [HttpGet("pallet-by-barcode")]
    public async Task<IActionResult> GetPalletByBarcode([FromQuery] string? barcode)
    {
        return FromResult(await _manager.GetPalletByBarcodeAsync(CurrentCompanyId, barcode));
    }

    [HttpGet("pallets/list")]
    public async Task<IActionResult> ListPallets([FromQuery] DispatchPalletPagedRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        var result = await _manager.GetPalletsPagedAsync(request);
        if (!result.IsSuccess)
            return FromResult(result);
        return Ok(result.Data);
    }

    [HttpGet("unpalletized-boxes")]
    public async Task<IActionResult> GetUnpalletizedBoxes()
    {
        return FromResult(await _manager.GetUnpalletizedBoxesAsync(CurrentCompanyId));
    }

    [HttpPost("pallets/add-box")]
    public async Task<IActionResult> AddBoxToPallet([FromBody] AddBoxToPalletRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        return FromResult(await _manager.AddBoxToPalletAsync(request));
    }

    [HttpPost("pallets/remove-box")]
    public async Task<IActionResult> RemoveBoxFromPallet([FromBody] RemoveBoxFromPalletRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        return FromResult(await _manager.RemoveBoxFromPalletAsync(request));
    }

    [HttpPost("pallets/mark-ready")]
    public async Task<IActionResult> MarkPalletReady([FromBody] PalletActionRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        return FromResult(await _manager.MarkPalletReadyAsync(request));
    }

    [HttpPost("pallets/mark-shipped")]
    public async Task<IActionResult> MarkPalletShipped([FromBody] PalletActionRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        return FromResult(await _manager.MarkPalletShippedAsync(request));
    }
}
