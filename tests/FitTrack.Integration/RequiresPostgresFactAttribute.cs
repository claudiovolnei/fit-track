namespace FitTrack.Integration;

/// <summary>
/// Marks a test that needs a real PostgreSQL instance. It is skipped unless
/// <c>FITTRACK_TEST_DB</c> holds a connection string, so the suite stays green on
/// machines without a database while still proving the adapter wherever one exists.
/// </summary>
public sealed class RequiresPostgresFactAttribute : FactAttribute
{
    public const string ConnectionStringVariable = "FITTRACK_TEST_DB";

    public RequiresPostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionStringVariable)))
        {
            Skip = $"Set {ConnectionStringVariable} to a PostgreSQL connection string to run integration tests.";
        }
    }
}

/// <summary>
/// The same rule for a test driven by a table of cases: the table is walked over PostgreSQL, so
/// without a database there is nothing to walk it against.
/// </summary>
public sealed class RequiresPostgresTheoryAttribute : TheoryAttribute
{
    public RequiresPostgresTheoryAttribute()
    {
        if (string.IsNullOrWhiteSpace(
            Environment.GetEnvironmentVariable(RequiresPostgresFactAttribute.ConnectionStringVariable)))
        {
            Skip = $"Set {RequiresPostgresFactAttribute.ConnectionStringVariable} to a PostgreSQL connection string to run integration tests.";
        }
    }
}
