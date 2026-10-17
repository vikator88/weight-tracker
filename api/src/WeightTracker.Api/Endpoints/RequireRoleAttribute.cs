using WeightTracker.Domain.Users;

namespace WeightTracker.Api.Endpoints;

/// <summary>
/// Restricts an action of an <see cref="AuthorizedController"/> to callers whose stored role is one
/// of the given roles. Used instead of <c>[Authorize(Roles = ...)]</c>, which the framework
/// enforces before the caller is looked up and would answer <c>403</c> for a user that no longer
/// exists, where <c>401</c> is due.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequireRoleAttribute : Attribute
{
    public RequireRoleAttribute(params Role[] roles)
    {
        Roles = roles;
    }

    public IReadOnlyList<Role> Roles { get; }
}
