using AutoFixture;
using AutoFixture.AutoMoq;

namespace WebMessenger.Api.Tests.Shared.TestData;

public static class FixtureFactory
{
    public static IFixture Create()
    {
        var fixture = new Fixture();
        fixture.Customize(new AutoMoqCustomization { ConfigureMembers = true });
        return fixture;
    }
}
