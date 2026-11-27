namespace Nexo.Discovery.Application.Directory;

/// <summary>
/// Guesses the catalog category behind free text. Tried in registration order (ML.NET first, generative model last);
/// returns null when unsure. Must never throw.
/// </summary>
public interface ISearchIntentModel
{
    string Source { get; }
    Task<string?> ClassifyAsync(string text, IReadOnlyList<string> slugs, CancellationToken ct);
}

/// <summary>What the search understood when the words matched no profile ("se me tapó el baño" → plomeria).</summary>
public sealed record SearchInterpretation(string Category, string Label, string Source);
