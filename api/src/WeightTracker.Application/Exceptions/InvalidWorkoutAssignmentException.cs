namespace WeightTracker.Application.Exceptions;

/// <summary>
/// Raised when the owner or trainer of a new workout, as requested, is not allowed for the caller's role.
/// </summary>
public sealed class InvalidWorkoutAssignmentException : ApplicationLayerException
{
    public InvalidWorkoutAssignmentException(string reason) : base(reason)
    { }
}
