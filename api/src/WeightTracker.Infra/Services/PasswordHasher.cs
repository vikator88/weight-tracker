using Microsoft.AspNetCore.Identity;
using WeightTracker.Application.Interfaces;

namespace WeightTracker.Infra.Services;

/// <summary>
/// PBKDF2 hashing backed by the in-box ASP.NET Core hasher.
/// </summary>
/// <remarks>
/// Passwords are never stored in plain text. The generic parameter of the underlying
/// hasher is unused by the algorithm, so a plain object stands in for the user.
/// </remarks>
public class PasswordHasher : IPasswordHasher
{
    private static readonly object HashedSubject = new();

    private readonly PasswordHasher<object> _hasher = new();

    public string Hash(string plainPassword) => _hasher.HashPassword(HashedSubject, plainPassword);

    public bool Verify(string plainPassword, string passwordHash)
    {
        if (string.IsNullOrEmpty(passwordHash) || string.IsNullOrEmpty(plainPassword))
            return false;

        try
        {
            var result = _hasher.VerifyHashedPassword(HashedSubject, passwordHash, plainPassword);

            return result is PasswordVerificationResult.Success
                or PasswordVerificationResult.SuccessRehashNeeded;
        }
        catch (FormatException)
        {
            // A stored value that is not a valid hash must fail verification, not crash the request
            return false;
        }
    }
}
