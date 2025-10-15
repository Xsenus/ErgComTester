using System;
using System.Threading;
using System.Windows.Forms;
using MicroluxErgConnect.Infrastructure;
using MicroluxErgConnect.Views;

namespace MicroluxErgConnect;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();
        SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());

        try
        {
            AppServices.Initialize();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"Критическая ошибка при инициализации приложения:\n{ex}",
                "Microlux ERG-Connect",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        if (AppServices.Log is not null)
        {
            Application.ThreadException += (_, args) =>
            {
                AppServices.Log.Error($"Необработанное исключение UI-потока: {args.Exception}");
            };

            AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            {
                if (args.ExceptionObject is Exception exception)
                {
                    AppServices.Log.Error($"Необработанное исключение домена приложений: {exception}");
                }
                else
                {
                    AppServices.Log.Error("Необработанная ошибка домена приложений без объекта исключения.");
                }
            };
        }

        using var mainForm = new MainForm();
        Application.Run(mainForm);

        AppServices.Dispose();
    }
}
