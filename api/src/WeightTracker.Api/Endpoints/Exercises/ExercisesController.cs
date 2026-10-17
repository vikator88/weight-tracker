using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeightTracker.Api.Common;
using WeightTracker.Api.Endpoints.Exercises.Dtos;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Application.UseCases.Exercises;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Users;

namespace WeightTracker.Api.Endpoints.Exercises;

/// <summary>
/// Reading exercises is public. Creating, replacing and deleting them is reserved to admins.
/// </summary>
/// <remarks>
/// <see cref="AuthorizedController"/> requires authentication by default, so the public reads opt
/// out with <c>[AllowAnonymous]</c> per action rather than on the class: an <c>[AllowAnonymous]</c>
/// on the class would also skip the caller lookup and the <c>[RequireRole]</c> of the write actions.
/// </remarks>
[ApiController]
[Route("exercises")]
public class ExercisesController : AuthorizedController
{
    private readonly GetAllExercisesUseCase _getAll;
    private readonly GetExerciseByIdUseCase _getById;
    private readonly CreateExerciseUseCase _create;
    private readonly UpdateExerciseUseCase _update;
    private readonly DeleteExerciseUseCase _delete;

    public ExercisesController(
        GetAllExercisesUseCase getAll,
        GetExerciseByIdUseCase getById,
        CreateExerciseUseCase create,
        UpdateExerciseUseCase update,
        DeleteExerciseUseCase delete,
        GetAuthenticatedUserUseCase getAuthenticatedUser,
        ExceptionMapper exceptionMapper) : base(getAuthenticatedUser, exceptionMapper)
    {
        _getAll = getAll;
        _getById = getById;
        _create = create;
        _update = update;
        _delete = delete;
    }

    [HttpGet]
    [AllowAnonymous]
    [ProducesResponseType<IReadOnlyList<ExerciseResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        try
        {
            var exercises = await _getAll.Execute(cancellationToken);

            return Ok(exercises.Select(ExerciseResponse.From).ToList());
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    [ProducesResponseType<ExerciseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var exercise = await _getById.Execute(Id.From(id), cancellationToken);

            return Ok(ExerciseResponse.From(exercise));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    [HttpPost]
    [RequireRole(Role.ADMIN)]
    [ProducesResponseType<ExerciseResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(
        [FromBody] ExerciseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var exercise = await _create.Execute(request.ToDto(), cancellationToken);

            return CreatedAtAction(
                nameof(GetById), new { id = exercise.Id.Value }, ExerciseResponse.From(exercise));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    /// <summary>
    /// Replaces every attribute of an existing exercise. It never creates one.
    /// </summary>
    [HttpPut("{id:guid}")]
    [RequireRole(Role.ADMIN)]
    [ProducesResponseType<ExerciseResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] ExerciseRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var exercise = await _update.Execute(Id.From(id), request.ToDto(), cancellationToken);

            return Ok(ExerciseResponse.From(exercise));
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }

    [HttpDelete("{id:guid}")]
    [RequireRole(Role.ADMIN)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            await _delete.Execute(Id.From(id), cancellationToken);

            return NoContent();
        }
        catch (Exception exception)
        {
            return ExceptionMapper.Map(exception);
        }
    }
}
