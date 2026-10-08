namespace WeightTracker.Application.Interfaces;

/// <summary>
/// Hashing of user credentials. Implemented in infrastructure so the domain never
/// depends on a hashing framework and never holds a plain-text password.
/// </summary>
public interface IPasswordHasher
{
    string Hash(string plainPassword);

    bool Verify(string plainPassword, string passwordHash);
}
