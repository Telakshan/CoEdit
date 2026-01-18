namespace CoEdit.Shared.Kernel.Abstractions;

public class DomainException: Exception
{
    protected DomainException(string message): base(message){}
}