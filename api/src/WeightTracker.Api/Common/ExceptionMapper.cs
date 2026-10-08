using Microsoft.AspNetCore.Mvc;
using WeightTracker.Application.Exceptions;
using WeightTracker.Domain.Exceptions;

namespace WeightTracker.Api.Common;

/// <summary>
/// Turns the custom exceptions raised by the Domain and Application layers into HTTP responses.
/// </summary>
/// <remarks>
/// Anything that is not a declared custom exception resolves to a 500, as does any custom
/// Application exception without an explicit mapping, since those signal a server-side fault
/// rather than bad caller input.
/// </remarks>
public class ExceptionMapper
{
    private readonly ILogger<ExceptionMapper> _logger;

    public ExceptionMapper(ILogger<ExceptionMapper> logger)
    {
        _logger = logger;
    }

    public IActionResult Map(Exception exception)
    {
        return exception switch
        {
            InvalidEmailException
                or InvalidIdException
                or InvalidPersonNameException
                or InvalidPasswordException
                or InvalidPasswordHashException
                or TrainerCannotBeWorkoutOwnerException
                => Problem(StatusCodes.Status400BadRequest, exception.Message),

            InvalidCredentialsException
                or InvalidRefreshTokenException
                => Problem(StatusCodes.Status401Unauthorized, exception.Message),

            UnauthorizedOperationException
                => Problem(StatusCodes.Status403Forbidden, exception.Message),

            UserNotFoundException
                or ExerciseNotInWorkoutException
                => Problem(StatusCodes.Status404NotFound, exception.Message),

            RefreshTokenAlreadyRevokedException
                => Problem(StatusCodes.Status409Conflict, exception.Message),

            // Any other domain rule breach is still a caller problem
            DomainException
                => Problem(StatusCodes.Status400BadRequest, exception.Message),

            _ => InternalServerError(exception),
        };
    }

    private IActionResult InternalServerError(Exception exception)
    {
        _logger.LogError(exception, "Unhandled exception while serving the request");

        return Problem(StatusCodes.Status500InternalServerError, "An unexpected error occurred");
    }

    private static IActionResult Problem(int statusCode, string detail)
        => new ObjectResult(new ProblemDetails { Status = statusCode, Detail = detail })
        {
            StatusCode = statusCode,
        };
}
