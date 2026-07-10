namespace AkilliDepo.Api.Entities;

public class Location : BaseEntity
{
    public int CorridorNo { get; set; }
    public int ZoneNo { get; set; }
    public int ShelfNo { get; set; }
    public string Barcode { get; set; } = string.Empty;
    public bool IsOccupied { get; set; }
    public int? CurrentBoxId { get; set; }

    public Box? CurrentBox { get; set; }
}
