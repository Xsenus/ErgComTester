using System.Windows;
using MicroluxErgConnect.Infrastructure;

namespace MicroluxErgConnect;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AppServices.Initialize();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        AppServices.Dispose();
        base.OnExit(e);
    }
}
