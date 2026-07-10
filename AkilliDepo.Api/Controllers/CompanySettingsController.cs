using AkilliDepo.Api.DTOs;
using AkilliDepo.Api.Entities;
using AkilliDepo.Api.Managers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AkilliDepo.Api.Controllers;

[Route("api/company-settings")]
public class CompanySettingsController : BaseApiController
{
    private readonly ICompanySettingsManager _manager;

    public CompanySettingsController(ICompanySettingsManager manager)
    {
        _manager = manager;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        return FromResult(await _manager.GetAsync(CurrentCompanyId));
    }

    [HttpPost("update")]
    [Authorize(Roles = UserRole.Admin)]
    public async Task<IActionResult> Update([FromBody] UpdateCompanySettingsRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        return FromResult(await _manager.UpdateAsync(request));
    }
}
