using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Exceptions;

public sealed class UserNotFoundException : ApplicationLayerException
{
    public UserNotFoundException(Id userId) : base($"User '{userId}' was not found")
    { }
}
