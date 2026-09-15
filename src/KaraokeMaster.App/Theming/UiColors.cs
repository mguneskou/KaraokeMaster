namespace KaraokeMaster.App.Theming;

/// <summary>
/// The app's dark palette. Extends PerformerForm's existing black/gold/white stage theme
/// (background black, accent gold) into something usable for a control-room UI viewed for
/// long stretches: near-black rather than pure black, with layered surface tones instead of
/// flat black-on-black.
/// </summary>
public static class UiColors
{
    public static readonly Color WindowBackground = Color.FromArgb(18, 18, 18);
    public static readonly Color Surface = Color.FromArgb(28, 28, 30);
    public static readonly Color SurfaceRaised = Color.FromArgb(38, 38, 41);
    public static readonly Color SurfaceHover = Color.FromArgb(50, 50, 54);
    public static readonly Color SurfacePressed = Color.FromArgb(64, 52, 16);

    public static readonly Color Accent = Color.FromArgb(255, 200, 40);
    public static readonly Color AccentDim = Color.FromArgb(150, 118, 24);

    public static readonly Color TextPrimary = Color.FromArgb(240, 240, 240);
    public static readonly Color TextSecondary = Color.FromArgb(170, 170, 176);
    public static readonly Color TextDisabled = Color.FromArgb(105, 105, 110);

    public static readonly Color Border = Color.FromArgb(58, 58, 62);
    public static readonly Color SplitterBackground = Color.FromArgb(12, 12, 12);

    public static readonly Color Error = Color.FromArgb(235, 100, 100);
}
