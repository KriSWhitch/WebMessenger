using Bogus;

namespace WebMessenger.Api.Tests.Shared.TestData;

public static class DeterministicBogus
{
    /// <summary>
    /// Creates a per-instance seeded <see cref="Faker{T}"/>.
    /// Uses <c>UseSeed()</c> to avoid mutating the global <c>Randomizer.Seed</c>,
    /// which would cause non-determinism when tests run in parallel.
    /// </summary>
    public static Faker<T> Create<T>(int seed = 1337) where T : class =>
        new Faker<T>().UseSeed(seed);
}
