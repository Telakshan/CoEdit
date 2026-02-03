using CoEdit.Shared.Kernel.Abstractions;

namespace User.Domain.Exceptions;

public class UserDomainException: DomainException
{
    public UserDomainException(string message) : base(message)
    {
    }
}