using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeightTracker.Api.Common;
using WeightTracker.Api.Endpoints.Workouts.Dtos;
using WeightTracker.Application.UseCases.Workouts;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;

namespace WeightTracker.Api.Endpoints.Workouts;

[ApiController]
[Route("workouts")]
[Authorize]
public class WorkoutsController : ControllerBase
{
    private readonly GetAllWorkoutsUseCase _getAllWorkouts;
    private readonly ExceptionMapper _exceptionMapper;

    public WorkoutsController(
        GetAllWorkoutsUseCase getAllWorkouts,
        ExceptionMapper exceptionMapper)
    {
        _getAllWorkouts = getAllWorkouts;
        _exceptionMapper = exceptionMapper;
    }

    /// <summary>
    /// Lists the workouts the caller is entitled to see. The scoping rule itself lives in
    /// the use case; the controller only resolves who the caller is.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<WorkoutResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        if (TryReadCaller(out var callerId, out var callerRole) == false)
            return Unauthorized();

        try
        {
            var workouts = await _getAllWorkouts.Execute(callerId!, callerRole, cancellationToken);

            return Ok(workouts.Select(WorkoutResponse.From).ToList());
        }
        catch (Exception exception)
        {
            return _exceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Reads the caller identity out of the validated token. A token without a usable
    /// subject or role is an authentication failure, not a domain error.
    /// </summary>
    private bool TryReadCaller(out Id? callerId, out Role callerRole)
    {
        callerRole = default;

        if (Id.TryFrom(User.FindFirstValue("sub"), out callerId) == false)
            return false;

        return Enum.TryParse(User.FindFirstValue(ClaimTypes.Role), ignoreCase: false, out callerRole)
            && Enum.IsDefined(callerRole);
    }
}
