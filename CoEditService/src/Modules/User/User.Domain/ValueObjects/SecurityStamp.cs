using CoEdit.Common.Domain.Abstractions;

namespace User.Domain.ValueObjects;

public class SecurityStamp: ValueObject
{
    private Guid Value { get; }

    private SecurityStamp(Guid value)
    {
        Value = value;
    }

    public static SecurityStamp Create()
    {
        return new SecurityStamp(Guid.NewGuid());
    }

    public static SecurityStamp From(Guid value)
    {
        return value == Guid.Empty ? throw new ArgumentException("Security stamp cannot be empty.") : new SecurityStamp(value);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}