using Nexo.Common.Errors;

namespace Nexo.Common.Unit;

/// <summary>An unmapped code falls through to 500, so every business code in use must have its own status.</summary>
public sealed class NexoProblemTests
{
    [Theory]
    [InlineData(NexoCodes.OrderActiveExists, 409)]
    [InlineData(NexoCodes.OrderRequestAlreadyAssigned, 409)]
    [InlineData(NexoCodes.OrderInvalidTransition, 409)]
    [InlineData(NexoCodes.OrderVersionConflict, 412)]
    [InlineData(NexoCodes.OrderPriceOutOfRange, 400)]
    [InlineData(NexoCodes.OrderAttentionNotSupported, 400)]
    [InlineData(NexoCodes.ReviewAlreadyExists, 409)]
    [InlineData(NexoCodes.ReviewOrderNotResolved, 403)]
    [InlineData(NexoCodes.ReviewStarsInvalid, 400)]
    [InlineData(NexoCodes.SavedOwnContent, 403)]
    public void Codigo_de_negocio_no_cae_en_500(string code, int status) => Assert.Equal(status, NexoProblem.Describe(code).Status);
}
