using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WeightTracker.Api.Common;
using WeightTracker.Api.Endpoints.Exercises.Dtos;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Application.UseCases.Exercises;
using WeightTracker.Domain.Common;

namespace WeightTracker.Api.Endpoints.Exercises;

/// <summary>
/// Exercises are a read-only catalog: reading them is public, and they are maintained in the database,
/// never through the API.
/// </summary>
/// <remarks>
/// <see cref="AuthorizedController"/> requires authentication by default, so the public reads opt
/// out with <c>[AllowAnonymous]</c> per action rather than on the class: an <c>[AllowAnonymous]</c>
/// on the class would also skip the caller lookup for any protected action added later.
/// </remarks>
[ApiController]
[Route("exercises")]
public class ExercisesController : AuthorizedController
{
    private readonly GetAllExercisesUseCase _getAll;
    private readonly GetExerciseByIdUseCase _getById;

    public ExercisesController(
        GetAllExercisesUseCase getAll,
        GetExerciseByIdUseCase getById,
        GetAuthenticatedUserUseCase getAuthenticatedUser,
        ExceptionMapper exceptionMapper) : base(getAuthenticatedUser, exceptionMapper)
    {
        _getAll = getAll;
        _getById = getById;
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
}
