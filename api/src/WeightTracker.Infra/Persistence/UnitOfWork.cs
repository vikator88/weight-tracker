using WeightTracker.Application.Common;

namespace WeightTracker.Infra.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly WeightTrackerDbContext _context;

    public UnitOfWork(WeightTrackerDbContext context)
    {
        _context = context;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
        => _context.SaveChangesAsync(cancellationToken);
}
