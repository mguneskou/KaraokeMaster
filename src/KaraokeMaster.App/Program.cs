using KaraokeMaster.App.Forms;

namespace KaraokeMaster.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new ControlForm());
    }
}
