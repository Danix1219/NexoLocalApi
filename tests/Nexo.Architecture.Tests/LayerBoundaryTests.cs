using NetArchTest.Rules;
using Nexo.Discovery.Application;
using Nexo.Discovery.Domain;
using Nexo.Discovery.Endpoints;
using Nexo.Discovery.Infrastructure;
using Nexo.Engagement.Application;
using Nexo.Engagement.Domain;
using Nexo.Engagement.Endpoints;
using Nexo.Engagement.Infrastructure;
using Nexo.Identity.Application;
using Nexo.Identity.Domain;
using Nexo.Identity.Endpoints;
using Nexo.Identity.Infrastructure;

namespace Nexo.Architecture.Tests;

public sealed class LayerBoundaryTests
{
    [Fact]
    public void Domain_does_not_depend_on_outer_layers()
    {
        AssertClean(Types.InAssembly(typeof(IdentityDomainMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Nexo.Identity.Application",
                "Nexo.Identity.Infrastructure",
                "Nexo.Identity.Endpoints",
                "Nexo.Identity.Contracts"));

        AssertClean(Types.InAssembly(typeof(DiscoveryDomainMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Nexo.Discovery.Application",
                "Nexo.Discovery.Infrastructure",
                "Nexo.Discovery.Endpoints",
                "Nexo.Discovery.Contracts"));

        AssertClean(Types.InAssembly(typeof(EngagementDomainMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny(
                "Nexo.Engagement.Application",
                "Nexo.Engagement.Infrastructure",
                "Nexo.Engagement.Endpoints",
                "Nexo.Engagement.Contracts"));
    }

    [Fact]
    public void Application_does_not_depend_on_infrastructure_or_endpoints()
    {
        AssertClean(Types.InAssembly(typeof(IdentityApplicationMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Nexo.Identity.Infrastructure", "Nexo.Identity.Endpoints"));

        AssertClean(Types.InAssembly(typeof(DiscoveryApplicationMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Nexo.Discovery.Infrastructure", "Nexo.Discovery.Endpoints"));

        AssertClean(Types.InAssembly(typeof(EngagementApplicationMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Nexo.Engagement.Infrastructure", "Nexo.Engagement.Endpoints"));
    }

    [Fact]
    public void Endpoints_do_not_depend_on_infrastructure()
    {
        AssertClean(Types.InAssembly(typeof(IdentityEndpointsMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Nexo.Identity.Infrastructure"));

        AssertClean(Types.InAssembly(typeof(DiscoveryEndpointsMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Nexo.Discovery.Infrastructure"));

        AssertClean(Types.InAssembly(typeof(EngagementEndpointsMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Nexo.Engagement.Infrastructure"));
    }

    [Fact]
    public void Infrastructure_does_not_depend_on_endpoints()
    {
        AssertClean(Types.InAssembly(typeof(IdentityInfrastructureMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Nexo.Identity.Endpoints"));

        AssertClean(Types.InAssembly(typeof(DiscoveryInfrastructureMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Nexo.Discovery.Endpoints"));

        AssertClean(Types.InAssembly(typeof(EngagementInfrastructureMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOn("Nexo.Engagement.Endpoints"));
    }

    [Fact]
    public void Domains_do_not_reference_other_domains_internals()
    {
        AssertClean(Types.InAssembly(typeof(IdentityDomainMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Nexo.Discovery", "Nexo.Engagement", "Nexo.Screens"));

        AssertClean(Types.InAssembly(typeof(DiscoveryDomainMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Nexo.Identity", "Nexo.Engagement", "Nexo.Screens"));

        AssertClean(Types.InAssembly(typeof(EngagementDomainMarker).Assembly)
            .ShouldNot()
            .HaveDependencyOnAny("Nexo.Identity", "Nexo.Discovery", "Nexo.Screens"));
    }

    private static void AssertClean(ConditionList conditions)
    {
        var result = conditions.GetResult();
        Assert.True(
            result.IsSuccessful,
            "Fronteras violadas: " + string.Join(", ", result.FailingTypeNames ?? []));
    }
}
