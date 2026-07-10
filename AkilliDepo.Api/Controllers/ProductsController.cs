using AkilliDepo.Api.DTOs;
using AkilliDepo.Api.Entities;
using AkilliDepo.Api.Managers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AkilliDepo.Api.Controllers;

[Route("api/products")]
public class ProductsController : BaseApiController
{
    private readonly IProductManager _manager;

    public ProductsController(IProductManager manager)
    {
        _manager = manager;
    }

    [HttpGet("list")]
    public async Task<IActionResult> List([FromQuery] ProductPagedRequest request)
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

    [HttpGet("by-barcode")]
    public async Task<IActionResult> GetByBarcode([FromQuery] string? barcode)
    {
        return FromResult(await _manager.GetByBarcodeAsync(CurrentCompanyId, barcode));
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        return FromResult(await _manager.CreateAsync(request));
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update([FromBody] UpdateProductRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        return FromResult(await _manager.UpdateAsync(request));
    }

    [HttpPost("bulk-create")]
    public async Task<IActionResult> BulkCreate([FromBody] BulkCreateProductsRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        return FromResult(await _manager.BulkCreateAsync(request));
    }

    [HttpPost("delete")]
    [Authorize(Roles = UserRole.Admin)]
    public async Task<IActionResult> Delete([FromBody] DeleteRequest request)
    {
        request.CompanyId = CurrentCompanyId;
        return FromResult(await _manager.DeleteAsync(request));
    }
}
