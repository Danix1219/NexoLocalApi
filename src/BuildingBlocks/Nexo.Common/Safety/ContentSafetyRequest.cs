namespace Nexo.Common.Safety;

/// <summary>Body of Azure AI Content Safety text:analyze, shared by every text moderator.</summary>
public static class ContentSafetyRequest
{
    /// <summary>Adds the profanity blocklist only when configured, so environments without it keep working.</summary>
    public static object Text(string text, string[] categories, string? blocklist) => string.IsNullOrWhiteSpace(blocklist)
        ? new { text, categories, outputType = "FourSeverityLevels" }
        : new { text, categories, outputType = "FourSeverityLevels", blocklistNames = new[] { blocklist.Trim() }, haltOnBlocklistHit = true };
}
