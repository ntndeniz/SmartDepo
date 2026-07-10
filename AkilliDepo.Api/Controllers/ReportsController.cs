using AkilliDepo.Api.DTOs;
using AkilliDepo.Api.Managers;
using Microsoft.AspNetCore.Mvc;

namespace AkilliDepo.Api.Controllers;

[Route("api/reports")]
public class ReportsController : BaseApiController
{
    private readonly IWeeklyReportManager _manager;

    public ReportsController(IWeeklyReportManager manager)
    {
        _manager = manager;
    }

    [HttpGet("list")]
    public async Task<IActionResult> List([FromQuery] ReportPagedRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        var result = await _manager.GetPagedAsync(request);
        if (!result.IsSuccess)
            return FromResult(result);
        return Ok(result.Data);
    }

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Download(int id)
    {
        var result = await _manager.DownloadAsync(id, CurrentCompanyId);
        if (!result.IsSuccess)
            return FromResult(result);

        var (fileName, content) = result.Data;
        return File(content, "text/csv", fileName);
    }

    [HttpPost("generate-now")]
    public async Task<IActionResult> GenerateNow([FromBody] GenerateReportNowRequest request)
    {
        await _manager.GenerateMissingWeeksAsync(CurrentCompanyId, DateTime.UtcNow);
        return Ok(ApiResponse<bool>.Ok(true, "Rapor üretimi tetiklendi."));
    }
}
