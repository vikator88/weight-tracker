using WeightTracker.Domain.Common;

namespace WeightTracker.Application.Exceptions;

public sealed class EmailAlreadyExistException : ApplicationLayerException
{
    public EmailAlreadyExistException(Email email)
        : base($"Email '{email}' is already registered") { }
}
