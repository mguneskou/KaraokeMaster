namespace KaraokeMaster.App.Theming;

/// <summary>
/// ApplicationIcon (csproj) only sets the .exe file's own icon (Explorer/taskbar shortcut) -
/// it doesn't automatically become each Form's title-bar/taskbar icon. Since the icon is already
/// embedded as the exe's Win32 resource, extracting it back out at runtime avoids also shipping
/// app.ico as a separate content file.
/// </summary>
public static class AppIcon
{
    public static Icon? TryLoad()
    {
        try
        {
            return Icon.ExtractAssociatedIcon(Application.ExecutablePath);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
