using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using WebMessenger.Api.Options;
using WebMessenger.Api.Services;
using WebMessenger.Api.Services.Interfaces;
using WebMessenger.Api.Tests.Shared;
using WebMessenger.Api.Tests.Shared.Mocks;
using WebMessenger.Api.Tests.Shared.TestData;
using WebMessenger.Contracts.Models;
using WebMessenger.DAL.Entities;
using WebMessenger.DAL.Interfaces;
using Xunit.Abstractions;

namespace WebMessenger.Api.Tests.Unit.Debugging;

/// <summary>
/// Practical debugging and logging showcase using <see cref="ITestOutputHelper"/>.
///
/// DEBUGGING PLAYBOOK
/// ==================
/// 1. Wrong mock setup
///    Symptom : test fails with "unexpected invocation" or returns null/default.
///    Fix     : ensure the mock Setup path matches exactly what the production code calls
///              (method name, parameter types, argument matchers).
///
/// 2. Shared fixture leakage
///    Symptom : test passes alone but fails in suite; order-dependent results.
///    Fix     : never mutate fixture state inside a test method.
///              Use IClassFixture for read-only shared data only.
///
/// 3. Async timing issues
///    Symptom : intermittent failures, assertions hit before async work completes.
///    Fix     : always await all async calls; never use .Result or .Wait() in tests.
///
/// 4. Non-deterministic test data
///    Symptom : tests are flaky across machines / runs.
///    Fix     : use DeterministicBogus.Create<T>(seed) or UserFaker with a fixed seed.
/// </summary>
public class DebuggingShowcaseTests(ITestOutputHelper output)
{
    private readonly TestLogger _log = new(output);

    // -------------------------------------------------------------------------
    // Example: logging mock interactions for debugging
    // -------------------------------------------------------------------------

    [Fact]
    public void MockInteraction_LogsCallsForDebugging()
    {
        // Arrange
        var contactsMock = new Mock<IContactsService>();
        var userId       = Guid.NewGuid();
        var contactId    = Guid.NewGuid();
        contactsMock.Setup(c => c.IsContact(userId, contactId)).Returns(true);

        _log.Log("Calling IsContact with userId={0}, contactId={1}", userId, contactId);

        // Act
        var result = contactsMock.Object.IsContact(userId, contactId);

        // Assert
        _log.Log("Result: {0}", result);
        Assert.True(result);

        // Verify — breakpoint-friendly: inspect invocations list in debugger here
        contactsMock.Verify(c => c.IsContact(userId, contactId), Times.Once);
        _log.Log("Verify passed — IsContact called exactly once");
    }

    // -------------------------------------------------------------------------
    // Example: diagnosing wrong-mock-setup scenario
    // -------------------------------------------------------------------------

    [Fact]
    public async Task WrongMockSetupDiagnosis_IncorrectArgMatcher_MockReturnsDefault()
    {
        // Arrange
        // BAD: setup uses a hardcoded Guid that differs from what the code actually passes.
        // This intentionally shows the pattern; we then use It.IsAny<> to fix it.
        var uowMock      = UnitOfWorkMockHelper.Create();
        var userRepoMock = new Mock<IRepository<User>>();
        var specificId   = Guid.NewGuid();
        var user         = UserFaker.Single();

        // Wrong setup: only matches specificId, but we'll call with a different id
        userRepoMock.Setup(r => r.GetAsync(specificId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        uowMock.Setup(u => u.UserRepository).Returns(userRepoMock.Object);
        _log.Log("Mock set up for specificId={0} only", specificId);

        // Act — call with a DIFFERENT id => mock returns null (no match)
        var differentId = Guid.NewGuid();
        var result = await uowMock.Object.UserRepository.GetAsync(differentId);

        // Assert — null because wrong setup (demonstrates the diagnosis pattern)
        _log.Log("GetAsync({0}) returned null because mock only matched {1}", differentId, specificId);
        Assert.Null(result);
    }

    // -------------------------------------------------------------------------
    // Example: deterministic vs non-deterministic data
    // -------------------------------------------------------------------------

    [Fact]
    public void DeterministicData_SameSeed_ProducesSameUsername()
    {
        // Arrange
        var user1 = UserFaker.Single(seed: 999);
        var user2 = UserFaker.Single(seed: 999);

        _log.Log("user1.Username = {0}", user1.Username);
        _log.Log("user2.Username = {0}", user2.Username);

        // Assert — same seed => same data => deterministic test
        Assert.Equal(user1.Username, user2.Username);
    }

    [Fact]
    public void DeterministicData_DifferentSeeds_ProduceDifferentUsernames()
    {
        // Arrange
        var user1 = UserFaker.Single(seed: 1);
        var user2 = UserFaker.Single(seed: 2);

        _log.Log("user1.Username = {0}, user2.Username = {1}", user1.Username, user2.Username);

        // Assert — different seeds => different data
        Assert.NotEqual(user1.Username, user2.Username);
    }

    // -------------------------------------------------------------------------
    // Example: async timing — always await
    // -------------------------------------------------------------------------

    [Fact]
    public async Task AsyncUsage_AlwaysAwait_NoTimingIssues()
    {
        // Arrange
        var uowMock      = UnitOfWorkMockHelper.Create();
        var userRepoMock = new Mock<IRepository<User>>();
        var user         = UserFaker.Single();

        userRepoMock.Setup(r => r.GetAll(It.IsAny<string[]>()))
            .Returns(new[] { user }.AsAsyncQueryable());
        uowMock.Setup(u => u.UserRepository).Returns(userRepoMock.Object);

        var jwtOptions = Microsoft.Extensions.Options.Options.Create(new JwtOptions
        {
            Key      = "super-secret-test-key-that-is-long-enough-32chars",
            Issuer   = "test-issuer",
            Audience = "test-audience"
        });
        var auth     = new AuthService(jwtOptions, NullLogger<AuthService>.Instance);
        var contacts = new Mock<IContactsService>().Object;
        var svc      = new UserService(uowMock.Object, contacts, auth, NullLogger<UserService>.Instance);

        _log.Log("Awaiting FindUserByUsernameAsync...");

        // Act — properly awaited, no race condition
        var result = await svc.FindUserByUsernameAsync(user.Username);

        // Assert
        _log.Log("Result username: {0}", result?.Username ?? "(null)");
        Assert.NotNull(result);
        Assert.Equal(user.Username, result.Username);
    }
}
