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
    private readonly CreateSuperSeriesUseCase _createSuperSeries;
    private readonly BreakSuperSeriesUseCase _breakSuperSeries;
    private readonly ExceptionMapper _exceptionMapper;

    public WorkoutsController(
        GetAllWorkoutsUseCase getAllWorkouts,
        CreateSuperSeriesUseCase createSuperSeries,
        BreakSuperSeriesUseCase breakSuperSeries,
        ExceptionMapper exceptionMapper)
    {
        _getAllWorkouts = getAllWorkouts;
        _createSuperSeries = createSuperSeries;
        _breakSuperSeries = breakSuperSeries;
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
            var result = await _getAllWorkouts.Execute(callerId!, callerRole, cancellationToken);

            return Ok(result.Workouts.Select(workout => WorkoutResponse.From(workout, result.Exercises)).ToList());
        }
        catch (Exception exception)
        {
            return _exceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Groups entries of the workout into a new superseries. <c>exerciseIds</c> are the <c>id</c> of
    /// each workout exercise entry, not the catalog <c>exerciseId</c>.
    /// </summary>
    /// <remarks>
    /// There is no route that reads a single superseries, so the response carries no <c>Location</c>.
    /// </remarks>
    [HttpPost("{workoutId:guid}/superseries")]
    [ProducesResponseType<SuperSeriesResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateSuperSeries(
        Guid workoutId, [FromBody] CreateSuperSeriesRequest request, CancellationToken cancellationToken)
    {
        if (TryReadCaller(out var callerId, out var callerRole) == false)
            return Unauthorized();

        try
        {
            var superSeriesId = await _createSuperSeries.Execute(
                callerId!,
                callerRole,
                Id.From(workoutId),
                request.ExerciseIds!.Select(Id.From).ToList(),
                cancellationToken);

            return StatusCode(StatusCodes.Status201Created, SuperSeriesResponse.From(superSeriesId));
        }
        catch (Exception exception)
        {
            return _exceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Ungroups a superseries of the workout. Its exercises stay in the workout.
    /// </summary>
    [HttpDelete("{workoutId:guid}/superseries/{superSeriesId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BreakSuperSeries(
        Guid workoutId, int superSeriesId, CancellationToken cancellationToken)
    {
        if (TryReadCaller(out var callerId, out var callerRole) == false)
            return Unauthorized();

        try
        {
            await _breakSuperSeries.Execute(callerId!, callerRole, Id.From(workoutId), superSeriesId, cancellationToken);

            return NoContent();
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
