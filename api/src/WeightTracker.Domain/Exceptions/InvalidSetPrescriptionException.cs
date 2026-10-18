namespace WeightTracker.Domain.Exceptions;

public sealed class InvalidSetPrescriptionException : DomainException
{
    public InvalidSetPrescriptionException(string reason)
        : base($"Invalid set prescription: {reason}")
    { }
}
