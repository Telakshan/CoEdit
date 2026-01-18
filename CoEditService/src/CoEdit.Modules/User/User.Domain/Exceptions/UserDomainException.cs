using CoEdit.Shared.Kernel.Common;

namespace User.Domain.Exceptions;

public class UserDomainException: DomainException
{
    public UserDomainException(string message) : base(message)
    {
    }
}