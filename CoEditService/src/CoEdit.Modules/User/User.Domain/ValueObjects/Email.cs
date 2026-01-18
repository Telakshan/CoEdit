using System.Text.RegularExpressions;

namespace User.Domain.ValueObjects;

public class Email
{
    private Email(string value)
    {
    }

    public static new Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be empty");
        }

        if (!Regex.IsMatch(email,  @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
        {
            throw new ArgumentException("Invalid email format!");
        }

        return new Email(email);
    }

}