using Xunit.Abstractions;

namespace WebMessenger.Api.Tests.Shared;

/// <summary>
/// Lightweight wrapper around <see cref="ITestOutputHelper"/> for structured diagnostic output.
/// Inject <c>ITestOutputHelper output</c> from xUnit, then wrap it here.
/// </summary>
public sealed class TestLogger(ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    public void Log(string message) => _output.WriteLine($"[{DateTime.UtcNow:HH:mm:ss.fff}] {message}");
    public void Log(string format, params object[] args) => Log(string.Format(format, args));
}
