using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using WeightTracker.Api.Common;
using WeightTracker.Application.Exceptions;
using WeightTracker.Domain.Common;
using WeightTracker.Domain.Exceptions;
using Xunit;

namespace WeightTracker.Integration.Common;

/// <summary>
/// Needs neither a database nor a host. It lives in this project because it is the one that
/// references the Api layer. Covers the status of every exception the exercises endpoints can
/// raise, including the ones no HTTP request can reach.
/// </summary>
public class ExceptionMapperTests
{
    private static int StatusOf(Exception exception)
    {
        var mapper = new ExceptionMapper(NullLogger<ExceptionMapper>.Instance);

        return ((ObjectResult)mapper.Map(exception)).StatusCode!.Value;
    }

    [Fact]
    public void Map_ShouldReturnBadRequestForAnInvalidExerciseName()
    {
        // Arrange
        var exception = new InvalidExerciseNameException("ab");

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void Map_ShouldReturnBadRequestForAnInvalidUrl()
    {
        // Arrange
        var exception = new InvalidUrlException("not a url");

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void Map_ShouldReturnBadRequestForAnInvalidBodyPart()
    {
        // Arrange
        var exception = new InvalidBodyPartException(99);

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public void Map_ShouldReturnNotFoundForAnUnknownExercise()
    {
        // Arrange
        var exception = new ExerciseNotFoundException(Id.New());

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public void Map_ShouldReturnConflictForAnExerciseInUse()
    {
        // Arrange
        var exception = new ExerciseInUseException(Id.New());

        // Act
        var status = StatusOf(exception);

        // Assert
        status.Should().Be(StatusCodes.Status409Conflict);
    }
}
