namespace WeightTracker.Application.Exceptions;

/// <summary>
/// Raised when a trainer asks for their athletes. There is no trainer–athlete relationship yet, so the case is
/// declared but deliberately not implemented. Remove it once that relationship exists.
/// </summary>
public sealed class TrainerAthletesNotImplementedException : ApplicationLayerException
{
    public TrainerAthletesNotImplementedException()
        : base("Listing a trainer's athletes is not implemented yet")
    { }
}
