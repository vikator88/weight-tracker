using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using WeightTracker.Api.Common;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Domain.Common;

namespace WeightTracker.Api.Endpoints;

/// <summary>
/// Base of every controller with protected routes. After <c>[Authorize]</c> has accepted the
/// token and before the action runs, it resolves the caller through
/// <see cref="GetAuthenticatedUserUseCase"/> and exposes it as <see cref="CurrentUser"/>.
/// </summary>
/// <remarks>
/// A token without a usable subject, or whose user no longer exists, ends the request with
/// <c>401</c> and the action never runs. Only then is <see cref="RequireRoleAttribute"/> checked
/// against the stored role, so a caller that is not who the token says is never answered with <c>403</c>. Actions marked <c>[AllowAnonymous]</c> are not
/// resolved and see a <c>null</c> <see cref="CurrentUser"/>.
/// </remarks>
[Authorize]
public abstract class AuthorizedController : ControllerBase, IAsyncActionFilter
{
    private readonly GetAuthenticatedUserUseCase _getAuthenticatedUser;

    protected AuthorizedController(GetAuthenticatedUserUseCase getAuthenticatedUser, ExceptionMapper exceptionMapper)
    {
        _getAuthenticatedUser = getAuthenticatedUser;
        ExceptionMapper = exceptionMapper;
    }

    protected ExceptionMapper ExceptionMapper { get; }

    /// <summary>
    /// The caller of the current request. <c>null</c> only on <c>[AllowAnonymous]</c> actions.
    /// </summary>
    protected AuthenticatedUser? CurrentUser { get; private set; }

    // Public, so MVC would otherwise discover it as a route
    [NonAction]
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context.ActionDescriptor.EndpointMetadata.OfType<IAllowAnonymous>().Any())
        {
            await next();
            return;
        }

        if (Id.TryFrom(User.FindFirst("sub")?.Value, out var callerId) == false)
        {
            context.Result = Unauthorized();
            return;
        }

        try
        {
            CurrentUser = await _getAuthenticatedUser.Execute(callerId!, HttpContext.RequestAborted);
        }
        catch (Exception exception)
        {
            context.Result = ExceptionMapper.Map(exception);
            return;
        }

        var requiredRoles = context.ActionDescriptor.EndpointMetadata.OfType<RequireRoleAttribute>().FirstOrDefault()?.Roles;

        if (requiredRoles is not null && requiredRoles.Contains(CurrentUser.Role) == false)
        {
            context.Result = StatusCode(StatusCodes.Status403Forbidden);
            return;
        }

        await next();
    }
}
