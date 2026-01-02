using CoEdit.Shared.Kernel;

namespace CoEdit.Modules.Collaboration.Models;

public class Document: Entity<Guid>
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public string Content { get; set; }
}