using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AppInstaller;

public sealed class AppEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("publisher")]
    public string Publisher { get; set; } = string.Empty;

    [JsonPropertyName("wingetId")]
    public string WingetId { get; set; } = string.Empty;

    [JsonPropertyName("officialUrl")]
    public string OfficialUrl { get; set; } = string.Empty;

    [JsonPropertyName("installerFileName")]
    public string InstallerFileName { get; set; } = string.Empty;

    [JsonPropertyName("installerType")]
    public string InstallerType { get; set; } = "exe";

    [JsonPropertyName("silentArgs")]
    public string SilentArgs { get; set; } = string.Empty;

    [JsonPropertyName("rebootSuppress")]
    public bool RebootSuppress { get; set; }

    [JsonPropertyName("needsUserInput")]
    public bool NeedsUserInput { get; set; }

    [JsonPropertyName("verifyMethod")]
    public string VerifyMethod { get; set; } = "signature";
}

public sealed class AppManifest
{
    [JsonPropertyName("apps")]
    public List<AppEntry> Apps { get; set; } = new();
}