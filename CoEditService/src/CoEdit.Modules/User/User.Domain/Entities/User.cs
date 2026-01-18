using CoEdit.Shared.Kernel;

namespace User.Domain.Entities;

public class User: Entity<Guid>
{
    public Email Email { get; set; }
    
}