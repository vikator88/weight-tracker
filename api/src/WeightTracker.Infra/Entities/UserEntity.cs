namespace WeightTracker.Infra.Entities;

/// <summary>EF representation of the <c>users</c> table.</summary>
public class UserEntity
{
    public Guid Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Surname { get; set; } = string.Empty;

    public DateOnly DateBirth { get; set; }

    public int Role { get; set; }

    public string PasswordHash { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }
}
