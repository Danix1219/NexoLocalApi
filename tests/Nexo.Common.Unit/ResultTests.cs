using Nexo.Common.Results;

namespace Nexo.Common.Unit;

public sealed class ResultTests
{
    [Fact]
    public void Success_exposes_value_without_error()
    {
        var result = Result<int>.Success(42);

        Assert.True(result.IsSuccess);
        Assert.False(result.IsFailure);
        Assert.Equal(42, result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_exposes_stable_code_without_value()
    {
        var result = Result<int>.Failure("identity.invalid_credentials", "Credenciales invalidas.");

        Assert.False(result.IsSuccess);
        Assert.True(result.IsFailure);
        Assert.Equal("identity.invalid_credentials", result.Error!.Code);
        Assert.Equal(0, result.Value);
    }

    [Fact]
    public void Match_invokes_only_the_selected_branch()
    {
        var succeeded = Result<string>.Success("ok").Match(_ => "success", _ => "failure");
        var failed = Result<string>.Failure("x", "y").Match(_ => "success", _ => "failure");

        Assert.Equal("success", succeeded);
        Assert.Equal("failure", failed);
    }
}
