namespace CoEdit.Shared.Kernel;

public interface IAuditableEntity
{
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    string? CreatedBy { get; set; }
    string? UpdatedBy { get; set; }
}