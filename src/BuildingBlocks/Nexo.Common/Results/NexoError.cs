namespace Nexo.Common.Results;

/// <summary>Error estable y serializable; el codigo no cambia entre versiones del contrato.</summary>
public sealed record NexoError(string Code, string Message);
