using Xunit;

// The integration suite shares one PostgreSQL database. Running test classes in parallel makes
// two hosts apply the same migrations and seed concurrently, which races on a fresh database.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
