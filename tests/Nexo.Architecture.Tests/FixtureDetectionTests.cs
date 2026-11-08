using NetArchTest.Rules;
using Nexo.Fixtures.BadDiscovery;
using Nexo.Fixtures.GoodDiscovery;

namespace Nexo.Architecture.Tests;

public sealed class FixtureDetectionTests
{
    [Fact]
    public void Rule_rejects_a_consumer_that_reaches_another_domain_internals()
    {
        var result = Types.InAssembly(typeof(BadDiscoveryFixture).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Nexo.Discovery.Domain")
            .GetResult();

        Assert.False(result.IsSuccessful);
    }

    [Fact]
    public void Rule_accepts_a_consumer_that_only_uses_the_public_contract()
    {
        var result = Types.InAssembly(typeof(GoodDiscoveryFixture).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Nexo.Discovery.Domain",
                "Nexo.Discovery.Application",
                "Nexo.Discovery.Infrastructure",
                "Nexo.Discovery.Endpoints")
            .GetResult();

        Assert.True(result.IsSuccessful);
    }
}
