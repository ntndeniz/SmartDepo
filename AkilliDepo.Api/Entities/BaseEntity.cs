namespace AkilliDepo.Api.Entities;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public string CompanyId { get; set; } = string.Empty;
    public bool IsDeleted { get; set; }
}
