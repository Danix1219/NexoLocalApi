namespace Nexo.Fixtures.GoodDiscovery;

/// <summary>Caso positivo: un consumidor que solo referencia el contrato publico de otro dominio.</summary>
public static class GoodDiscoveryFixture
{
    public static string ContractName => typeof(Nexo.Discovery.Contracts.DiscoveryContractsMarker).FullName!;
}
