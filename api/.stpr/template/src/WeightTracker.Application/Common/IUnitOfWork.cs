namespace WeightTracker.Application.Common;

/// <summary>
/// Commits the work accumulated by the repositories during one operation.
/// </summary>
/// <remarks>
/// Repositories stage changes; they never commit. The use case decides the transaction
/// boundary and calls this once, at the end of a successful operation.
/// </remarks>
public interface IUnitOfWork
{
    public Task SaveChangesAsync(CancellationToken cancellationToken);
}
