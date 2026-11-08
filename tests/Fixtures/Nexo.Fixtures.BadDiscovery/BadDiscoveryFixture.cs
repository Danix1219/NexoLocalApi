namespace Nexo.Fixtures.BadDiscovery;

/// <summary>Caso negativo: un consumidor que referencia una capa interna de otro dominio.</summary>
public static class BadDiscoveryFixture
{
    public static string DomainName => typeof(Nexo.Discovery.Domain.DiscoveryDomainMarker).FullName!;
}
