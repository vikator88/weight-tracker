using WeightTracker.Domain.Common;

namespace WeightTracker.Domain.Users;

/// <summary>
/// A person using the system. Owns the credentials used by the authentication flow.
/// </summary>
/// <remarks>
/// The aggregate never sees a plain-text password: hashing is an infrastructure concern
/// and the already-hashed value is handed in by the Application layer, typed as
/// <see cref="Users.PasswordHash"/>. Strength rules live on <see cref="Password"/>, at the
/// boundary where a plain-text exists, and never reach this aggregate.
/// </remarks>
public sealed class User : AggregateRoot
{
    public Email Email { get; private set; }

    public PersonName Name { get; private set; }

    public PersonName Surname { get; private set; }

    public DateOnly DateBirth { get; private set; }

    public Role Role { get; private set; }

    public PasswordHash PasswordHash { get; private set; }

    public DateTime CreatedAt { get; private set; }

    private User(
        Id id,
        Email email,
        PersonName name,
        PersonName surname,
        DateOnly dateBirth,
        Role role,
        PasswordHash passwordHash,
        DateTime createdAt
    )
    {
        this.Id = id;
        Email = email;
        Name = name;
        Surname = surname;
        DateBirth = dateBirth;
        Role = role;
        PasswordHash = passwordHash;
        CreatedAt = createdAt;
    }

    /// <summary>
    /// Creates a brand new user.
    /// </summary>
    /// <param name="passwordHash">Password already hashed by the infrastructure hasher.</param>
    public static User Create(
        Email email,
        PersonName name,
        PersonName surname,
        DateOnly dateBirth,
        Role role,
        PasswordHash passwordHash,
        DateTime createdAtUtc
    )
    {
        return new User(Id.New(), email, name, surname, dateBirth, role, passwordHash, createdAtUtc)
        {
            IsNew = true,
        };
    }

    /// <summary>
    /// Restores a user already stored in the system.
    /// </summary>
    public static User Rehydrate(
        Id id,
        Email email,
        PersonName name,
        PersonName surname,
        DateOnly dateBirth,
        Role role,
        PasswordHash passwordHash,
        DateTime createdAt
    )
    {
        return new User(id, email, name, surname, dateBirth, role, passwordHash, createdAt)
        {
            IsNew = false,
        };
    }

    /// <summary>
    /// True when this user is allowed to be assigned as the trainer of a workout.
    /// </summary>
    public bool CanTrain() => Role is Role.TRAINER or Role.ADMIN;
}
