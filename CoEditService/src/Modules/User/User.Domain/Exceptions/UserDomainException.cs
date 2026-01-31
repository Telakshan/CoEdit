using CoEdit.Common.Domain.Exception;

namespace User.Domain.Exceptions;

public class UserDomainException(string message) : DomainException(message);