namespace WeightTracker.Application.Exceptions;

/// <summary>
/// Base type for every custom exception thrown by the Application layer.
/// Named with the <c>Layer</c> suffix to avoid colliding with <see cref="System.ApplicationException"/>.
/// </summary>
public abstract class ApplicationLayerException : Exception
{
    protected ApplicationLayerException(string message) : base(message)
    { }

    protected ApplicationLayerException(string message, Exception innerException) : base(message, innerException)
    { }
}
