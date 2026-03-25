namespace LoadNormalizer;

public record AutoUpdate
{
    public int Version { get; init; }
    public bool UpdateSettingsOnNewVersion { get; init; }

    public static int CurrentVersion { get; } = 3;
}
