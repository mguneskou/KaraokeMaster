namespace KaraokeMaster.Core;

/// <summary>
/// Resolves every runtime data location (database, separated-audio cache, python venv, settings)
/// under a single "data" folder next to the solution, so nothing writes to %LocalAppData% on C:.
/// </summary>
public static class AppPaths
{
    public static string Root { get; } = ResolveRoot();

    public static string DataRoot => EnsureExists(Path.Combine(Root, "data"));
    public static string DatabaseFile => Path.Combine(DataRoot, "library.db");
    public static string SettingsFile => Path.Combine(DataRoot, "settings.json");
    public static string SeparatedRoot => EnsureExists(Path.Combine(DataRoot, "Separated"));
    public static string PythonEnvRoot => Path.Combine(DataRoot, "pyenv");
    public static string TempRoot => EnsureExists(Path.Combine(DataRoot, "temp"));

    /// <summary>Hugging Face's own model cache (faster-whisper's download location), redirected off C:.</summary>
    public static string HuggingFaceCacheRoot => EnsureExists(Path.Combine(DataRoot, "hf-cache"));

    /// <summary>
    /// The bundled transcribe_lyrics.py, copied next to the built exe (both dev and published
    /// output) — resolved relative to the running assembly, not the data root, since it ships
    /// with the app rather than living in user-writable data.
    /// </summary>
    public static string TranscribeLyricsScriptPath =>
        Path.Combine(AppContext.BaseDirectory, "Scripts", "transcribe_lyrics.py");

    public static string SeparatedFolderFor(int songId) =>
        EnsureExists(Path.Combine(SeparatedRoot, songId.ToString()));

    private static string ResolveRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            if (dir.GetFiles("KaraokeMaster.sln").Length > 0)
            {
                return dir.FullName;
            }
            dir = dir.Parent;
        }

        // Deployed/published scenario: no .sln alongside the exe, fall back to a
        // "data" folder next to the executable rather than %LocalAppData%.
        return AppContext.BaseDirectory;
    }

    private static string EnsureExists(string path)
    {
        Directory.CreateDirectory(path);
        return path;
    }
}
