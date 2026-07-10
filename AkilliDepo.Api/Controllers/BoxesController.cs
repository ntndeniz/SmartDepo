using AkilliDepo.Api.DTOs;
using AkilliDepo.Api.Managers;
using Microsoft.AspNetCore.Mvc;

namespace AkilliDepo.Api.Controllers;

[Route("api/boxes")]
public class BoxesController : BaseApiController
{
    private readonly IBoxManager _manager;

    public BoxesController(IBoxManager manager)
    {
        _manager = manager;
    }

    [HttpGet("list")]
    public async Task<IActionResult> List([FromQuery] BoxPagedRequest request)
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

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateBoxRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        request.CreatedBy = CurrentUsername;
        return FromResult(await _manager.CreateAsync(request));
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody] UpdateBoxRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        request.AdjustedBy = CurrentUsername;
        return FromResult(await _manager.UpdateAsync(request));
    }

    [HttpPost("delete")]
    public async Task<IActionResult> Delete([FromBody] DeleteRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        return FromResult(await _manager.DeleteAsync(request));
    }
}
