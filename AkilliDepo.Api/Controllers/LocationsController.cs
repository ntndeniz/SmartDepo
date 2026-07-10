using AkilliDepo.Api.DTOs;
using AkilliDepo.Api.Managers;
using Microsoft.AspNetCore.Mvc;

namespace AkilliDepo.Api.Controllers;

[Route("api/locations")]
public class LocationsController : BaseApiController
{
    private readonly ILocationManager _manager;

    public LocationsController(ILocationManager manager)
    {
        _manager = manager;
    }

    [HttpGet("list")]
    public async Task<IActionResult> List([FromQuery] LocationPagedRequest request)
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

    [HttpPost("generate")]
    public async Task<IActionResult> Generate([FromBody] GenerateLocationsRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        return FromResult(await _manager.GenerateAsync(request));
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody] DeleteRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        return FromResult(await _manager.DeleteAsync(request));
    }

    [HttpPost("assign-box")]
    public async Task<IActionResult> AssignBox([FromBody] AssignBoxRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        return FromResult(await _manager.AssignBoxAsync(request));
    }

    [HttpPost("release")]
    public async Task<IActionResult> Release([FromBody] ReleaseLocationRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        return FromResult(await _manager.ReleaseAsync(request));
    }
}
