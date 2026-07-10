namespace AkilliDepo.Api.Entities;

public class DispatchPalletBox : BaseEntity
{
    public int DispatchPalletId { get; set; }
    public int DispatchBoxId { get; set; }

    public DispatchPallet? DispatchPallet { get; set; }
    public DispatchBox? DispatchBox { get; set; }
}
