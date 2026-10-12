namespace WeightTracker.Application.Exceptions;

public sealed class AdminSignupNotAllowedException : ApplicationLayerException
{
    public AdminSignupNotAllowedException()
        : base("Admin accounts cannot be created through signup") { }
}
