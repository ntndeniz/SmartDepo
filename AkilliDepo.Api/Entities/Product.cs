namespace AkilliDepo.Api.Entities;

public static class ProductUnit
{
    public const string Adet = "Adet";
    public const string Kg = "Kg";
    public const string Koli = "Koli";
    public const string Paket = "Paket";

    public static readonly string[] All = { Adet, Kg, Koli, Paket };
}

public class Product : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
    public int BrandId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Brand? Brand { get; set; }
}
