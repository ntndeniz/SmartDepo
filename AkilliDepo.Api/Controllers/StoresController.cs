using AkilliDepo.Api.DTOs;
using AkilliDepo.Api.Entities;
using AkilliDepo.Api.Managers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AkilliDepo.Api.Controllers;

[Route("api/stores")]
public class StoresController : BaseApiController
{
    private readonly IStoreManager _manager;

    public StoresController(IStoreManager manager)
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

    [HttpPost("create")]
    [Authorize(Roles = UserRole.Admin)]
    public async Task<IActionResult> Create([FromBody] CreateStoreRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        return FromResult(await _manager.CreateAsync(request));
    }

    [HttpPost("update")]
    [Authorize(Roles = UserRole.Admin)]
    public async Task<IActionResult> Update([FromBody] UpdateStoreRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        return FromResult(await _manager.UpdateAsync(request));
    }

    [HttpPost("delete")]
    [Authorize(Roles = UserRole.Admin)]
    public async Task<IActionResult> Delete([FromBody] DeleteRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        return FromResult(await _manager.DeleteAsync(request));
    }
}
