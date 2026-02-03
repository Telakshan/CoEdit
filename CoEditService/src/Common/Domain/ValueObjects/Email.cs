using System.Text.RegularExpressions;
using CoEdit.Common.Domain.Abstractions;

namespace CoEdit.Common.Domain.ValueObjects;

public class Email: ValueObject
{
    private string Value { get; }

    protected Email(string value)
    {
        Value = value;
    }

    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be empty.");
        }

        // Simple regex for demonstration
        if (!Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            throw new ArgumentException("Invalid email format.");
        }

        return new Email(email);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Value;
    }
}