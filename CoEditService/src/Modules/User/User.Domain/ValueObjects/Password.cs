using CoEdit.Shared.Kernel.Abstractions;
using User.Domain.Abstractions;

namespace User.Domain.ValueObjects;

public class Password: ValueObject
{
    private string Hash { get; }

    private Password(string hash)
    {
        Hash = hash;
    }

    public static Password Create(string plainPassword, IPasswordHasher passwordHasher)
    {
        if (string.IsNullOrWhiteSpace(plainPassword))
        {
            throw new ArgumentException(plainPassword);
        }

        if (plainPassword.Length < 8)
        {
            throw new ArgumentException("Password must be at least 8 characters long.");
        }

        string hash = passwordHasher.Hash(plainPassword);
        return new Password(hash);
    }
    
    public static Password FromHash(string hash)
    {
        if (string.IsNullOrWhiteSpace(hash))
        {
            throw new ArgumentException("Password hash cannot be empty.");
        }
        return new Password(hash);
    }
    
    public bool Verify(string plainPassword, IPasswordHasher passwordHasher)
    {
        return passwordHasher.Verify(plainPassword, Hash);
    }

    public override IEnumerable<object> GetEqualityComponents()
    {
        yield return Hash;
    }
}