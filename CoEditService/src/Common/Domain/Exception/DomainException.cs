namespace CoEdit.Common.Domain.Exception;

public class DomainException: System.Exception
{
    protected DomainException(string message): base(message){}
}