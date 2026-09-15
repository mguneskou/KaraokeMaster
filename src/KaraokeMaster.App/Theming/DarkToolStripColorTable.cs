namespace KaraokeMaster.App.Theming;

/// <summary>Dark color table for MenuStrip/StatusStrip/ContextMenuStrip via ToolStripProfessionalRenderer.</summary>
public sealed class DarkToolStripColorTable : ProfessionalColorTable
{
    public override Color MenuStripGradientBegin => UiColors.Surface;
    public override Color MenuStripGradientEnd => UiColors.Surface;
    public override Color MenuItemSelected => UiColors.SurfaceHover;
    public override Color MenuItemSelectedGradientBegin => UiColors.SurfaceHover;
    public override Color MenuItemSelectedGradientEnd => UiColors.SurfaceHover;
    public override Color MenuItemPressedGradientBegin => UiColors.SurfacePressed;
    public override Color MenuItemPressedGradientEnd => UiColors.SurfacePressed;
    public override Color MenuItemBorder => UiColors.Accent;
    public override Color MenuBorder => UiColors.Border;
    public override Color ToolStripDropDownBackground => UiColors.Surface;
    public override Color ImageMarginGradientBegin => UiColors.Surface;
    public override Color ImageMarginGradientMiddle => UiColors.Surface;
    public override Color ImageMarginGradientEnd => UiColors.Surface;
    public override Color SeparatorDark => UiColors.Border;
    public override Color SeparatorLight => UiColors.Border;
    public override Color StatusStripGradientBegin => UiColors.Surface;
    public override Color StatusStripGradientEnd => UiColors.Surface;
    public override Color ToolStripBorder => UiColors.Border;
}
