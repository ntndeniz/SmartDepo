using AkilliDepo.Api.DTOs;
using AkilliDepo.Api.Entities;
using AkilliDepo.Api.Repositories;

namespace AkilliDepo.Api.Managers;

public interface IProductManager
{
    Task<ServiceResult<PagedResponse<ProductDto>>> GetPagedAsync(ProductPagedRequest request);
    Task<ServiceResult<ProductDto>> GetByIdAsync(int id, string? companyId);
    Task<ServiceResult<ProductDto>> GetByBarcodeAsync(string? companyId, string? barcode);
    Task<ServiceResult<ProductDto>> CreateAsync(CreateProductRequest request);
    Task<ServiceResult<ProductDto>> UpdateAsync(UpdateProductRequest request);
    Task<ServiceResult<bool>> DeleteAsync(DeleteRequest request);
    Task<ServiceResult<BulkCreateProductsResultDto>> BulkCreateAsync(BulkCreateProductsRequest request);
}

public class ProductManager : IProductManager
{
    private readonly IProductRepository _repository;
    private readonly IBrandRepository _brandRepository;
    private readonly IBoxRepository _boxRepository;

    public ProductManager(IProductRepository repository, IBrandRepository brandRepository, IBoxRepository boxRepository)
    {
        _repository = repository;
        _brandRepository = brandRepository;
        _boxRepository = boxRepository;
    }

    private static ProductDto ToDto(Product p) => new()
    {
        Id = p.Id,
        CompanyId = p.CompanyId,
        Name = p.Name,
        Barcode = p.Barcode,
        Unit = p.Unit,
        Color = p.Color,
        BrandId = p.BrandId,
        BrandName = p.Brand?.Name ?? string.Empty,
        CreatedAt = p.CreatedAt
    };

    public async Task<ServiceResult<PagedResponse<ProductDto>>> GetPagedAsync(ProductPagedRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<PagedResponse<ProductDto>>.BadRequest("CompanyId zorunludur.");

        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize < 1 ? 25 : request.PageSize;

        var (items, totalCount) = await _repository.GetPagedAsync(
            request.CompanyId, page, pageSize, request.Search, request.BrandId);

        return ServiceResult<PagedResponse<ProductDto>>.Ok(new PagedResponse<ProductDto>
        {
            Data = items.Select(ToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        });
    }

    public async Task<ServiceResult<ProductDto>> GetByIdAsync(int id, string? companyId)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return ServiceResult<ProductDto>.BadRequest("CompanyId zorunludur.");

        var product = await _repository.GetByIdAsync(id);
        if (product is null)
            return ServiceResult<ProductDto>.NotFound("Ürün bulunamadı.");
        if (product.CompanyId != companyId)
            return ServiceResult<ProductDto>.Forbidden("Bu ürüne erişim yetkiniz yok.");

        return ServiceResult<ProductDto>.Ok(ToDto(product));
    }

    public async Task<ServiceResult<ProductDto>> GetByBarcodeAsync(string? companyId, string? barcode)
    {
        if (string.IsNullOrWhiteSpace(companyId))
            return ServiceResult<ProductDto>.BadRequest("CompanyId zorunludur.");
        if (string.IsNullOrWhiteSpace(barcode))
            return ServiceResult<ProductDto>.BadRequest("Barkod zorunludur.");

        var product = await _repository.GetByBarcodeAsync(companyId, barcode);
        if (product is null)
            return ServiceResult<ProductDto>.NotFound("Bu barkoda sahip ürün bulunamadı.");

        return ServiceResult<ProductDto>.Ok(ToDto(product));
    }

    private static string GenerateBarcode(string brandName, string color, string productName)
    {
        var brandPrefix = BarcodeText.ToBarcodeSafeUpper(new string(brandName.Take(3).ToArray()));
        var colorChar = color.Length > 0 ? BarcodeText.ToBarcodeSafeUpper(color[0].ToString()) : "X";
        var productPrefix = productName.Length >= 2
            ? BarcodeText.ToBarcodeSafeUpper(new string(productName.Take(2).ToArray()))
            : BarcodeText.ToBarcodeSafeUpper(productName).PadRight(2, 'X');
        var random = new Random();
        var randomSuffix = random.Next(100, 999).ToString();
        return brandPrefix + colorChar + productPrefix + randomSuffix;
    }

    private async Task<string?> ValidateAsync(string companyId, string? name, string? color, string? unit, int brandId)
    {
        if (string.IsNullOrWhiteSpace(name))
            return "Ürün adı zorunludur.";
        if (string.IsNullOrWhiteSpace(color))
            return "Renk zorunludur.";
        if (string.IsNullOrWhiteSpace(unit) || !ProductUnit.All.Contains(unit))
            return $"Birim şu değerlerden biri olmalı: {string.Join(", ", ProductUnit.All)}.";

        var brand = await _brandRepository.GetByIdAsync(brandId);
        if (brand is null || brand.CompanyId != companyId)
            return "Geçerli bir marka seçmelisiniz.";

        return null;
    }

    public async Task<ServiceResult<ProductDto>> CreateAsync(CreateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<ProductDto>.BadRequest("CompanyId zorunludur.");

        var error = await ValidateAsync(request.CompanyId, request.Name, request.Color, request.Unit, request.BrandId);
        if (error is not null)
            return ServiceResult<ProductDto>.BadRequest(error);

        var brand = await _brandRepository.GetByIdAsync(request.BrandId);
        var generatedBarcode = GenerateBarcode(brand!.Name, request.Color!, request.Name!);

        var existing = await _repository.GetByBarcodeAsync(request.CompanyId, generatedBarcode);
        if (existing is not null)
            return ServiceResult<ProductDto>.BadRequest("Üretilen barkod çakışıyor, lütfen yeniden deneyin.");

        var product = new Product
        {
            CompanyId = request.CompanyId,
            Name = request.Name!,
            Barcode = generatedBarcode,
            Unit = request.Unit!,
            Color = request.Color!,
            BrandId = request.BrandId,
            CreatedAt = DateTime.UtcNow
        };

        await _repository.AddAsync(product);
        await _repository.SaveChangesAsync();

        var created = await _repository.GetByIdAsync(product.Id);
        return ServiceResult<ProductDto>.Ok(ToDto(created!), $"Ürün oluşturuldu. Barkod: {generatedBarcode}");
    }

    public async Task<ServiceResult<ProductDto>> UpdateAsync(UpdateProductRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<ProductDto>.BadRequest("CompanyId zorunludur.");

        var error = await ValidateAsync(request.CompanyId, request.Name, request.Color, request.Unit, request.BrandId);
        if (error is not null)
            return ServiceResult<ProductDto>.BadRequest(error);

        var product = await _repository.GetByIdAsync(request.Id);
        if (product is null)
            return ServiceResult<ProductDto>.NotFound("Ürün bulunamadı.");
        if (product.CompanyId != request.CompanyId)
            return ServiceResult<ProductDto>.Forbidden("Bu ürüne erişim yetkiniz yok.");

        product.Name = request.Name!;
        product.Unit = request.Unit!;
        product.Color = request.Color!;
        product.BrandId = request.BrandId;

        await _repository.UpdateAsync(product);
        await _repository.SaveChangesAsync();

        var updated = await _repository.GetByIdAsync(product.Id);
        return ServiceResult<ProductDto>.Ok(ToDto(updated!), "Ürün güncellendi.");
    }

    public async Task<ServiceResult<BulkCreateProductsResultDto>> BulkCreateAsync(BulkCreateProductsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<BulkCreateProductsResultDto>.BadRequest("CompanyId zorunludur.");
        if (request.Items.Count == 0)
            return ServiceResult<BulkCreateProductsResultDto>.BadRequest("Yüklenecek ürün satırı bulunamadı.");

        var brand = await _brandRepository.GetByIdAsync(request.BrandId);
        if (brand is null || brand.CompanyId != request.CompanyId)
            return ServiceResult<BulkCreateProductsResultDto>.BadRequest("Geçerli bir marka seçmelisiniz.");

        var result = new BulkCreateProductsResultDto();
        var rowNumber = 0;

        foreach (var item in request.Items)
        {
            rowNumber++;
            var row = new BulkCreateRowResultDto { RowNumber = rowNumber, Name = item.Name };

            var error = await ValidateAsync(request.CompanyId, item.Name, item.Color, item.Unit, request.BrandId);
            if (error is not null)
            {
                row.Error = error;
                result.Rows.Add(row);
                continue;
            }

            // Her satır kendi SaveChangesAsync'ini çağırır: barkod üretimi bir sonraki satırın
            // eşsizlik kontrolünde bu satırın barkodunu görmeli, aksi halde aynı toplu yüklemede
            // çakışan barkodlar üretilebilir.
            var generatedBarcode = GenerateBarcode(brand.Name, item.Color!, item.Name!);
            var existing = await _repository.GetByBarcodeAsync(request.CompanyId, generatedBarcode);
            if (existing is not null)
            {
                row.Error = "Üretilen barkod çakıştı, bu satırı tekrar deneyin.";
                result.Rows.Add(row);
                continue;
            }

            var product = new Product
            {
                CompanyId = request.CompanyId,
                Name = item.Name!,
                Barcode = generatedBarcode,
                Unit = item.Unit!,
                Color = item.Color!,
                BrandId = request.BrandId,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.AddAsync(product);
            await _repository.SaveChangesAsync();

            row.Success = true;
            row.Barcode = generatedBarcode;
            result.Rows.Add(row);
            result.CreatedCount++;
        }

        var message = $"{result.CreatedCount} / {request.Items.Count} ürün oluşturuldu.";
        return ServiceResult<BulkCreateProductsResultDto>.Ok(result, message);
    }

    public async Task<ServiceResult<bool>> DeleteAsync(DeleteRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CompanyId))
            return ServiceResult<bool>.BadRequest("CompanyId zorunludur.");

        var product = await _repository.GetByIdAsync(request.Id);
        if (product is null)
            return ServiceResult<bool>.NotFound("Ürün bulunamadı.");
        if (product.CompanyId != request.CompanyId)
            return ServiceResult<bool>.Forbidden("Bu ürüne erişim yetkiniz yok.");

        // Ürüne bağlı koliler varken silinirse, o koliler "ürünü silinmiş" olarak sistemde
        // görünmez hale gelirdi (fiziksel stok dururken kaybolan koli). Önce koliler kaldırılmalı.
        var hasBoxes = await _boxRepository.AnyByProductAsync(request.CompanyId, product.Id);
        if (hasBoxes)
            return ServiceResult<bool>.BadRequest("Bu ürüne bağlı koliler var. Önce ilgili kolileri silin/tüketin.");

        product.IsDeleted = true;
        await _repository.UpdateAsync(product);
        await _repository.SaveChangesAsync();

        return ServiceResult<bool>.Ok(true, "Ürün silindi.");
    }
}
