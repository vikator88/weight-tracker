using WeightTracker.Domain.Users;

namespace WeightTracker.Application.Interfaces;

/// <summary>
/// Hashing of user credentials. Implemented in infrastructure so the domain never
/// depends on a hashing framework and never holds a plain-text password.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a password that has already passed the strength rules carried by
    /// <see cref="Password"/>. This is the only boundary where a plain-text is accepted
    /// for storage.
    /// </summary>
    PasswordHash Hash(Password plainPassword);

    /// <summary>
    /// Verifies a submitted password against a stored hash.
    /// </summary>
    /// <param name="plainPassword">
    /// The submitted value, deliberately untyped. The login path must not run the strength
    /// rules over it: an existing credential that no longer satisfies them must still
    /// authenticate, and rejecting it would disclose which stored passwords are weak.
    /// </param>
    bool Verify(string plainPassword, PasswordHash passwordHash);
}
