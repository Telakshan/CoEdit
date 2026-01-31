using CoEdit.Common.Domain.Abstractions;

namespace CoEdit.Common.Domain.ValueObjects;

public class PhoneNumber: ValueObject
{
    private string Value { get; }

    private PhoneNumber(string value)
    {
        Value = value;
    }

    public static PhoneNumber Create(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            throw new ArgumentException("Phone number cannot be empty.");
        }

        return phoneNumber.Length < 7 ? throw new ArgumentException("Invalid phone number length.") : new PhoneNumber(phoneNumber);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }   
}