using Xunit;

namespace WeightTracker.Integration.Bootstrap;

/// <summary>
/// Shares one API host and one database across the suite. Tests run sequentially within
/// the collection, so resetting the database between them is safe.
/// </summary>
[CollectionDefinition(Name)]
public class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
