using System;
using System.Text;
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
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        ApplicationConfiguration.Initialize();

        const string mutexName = "Global\\MicroluxErgConnect";
        using var instanceMutex = new Mutex(initiallyOwned: false, mutexName);
        var mutexAcquired = false;

        try
        {
            try
            {
                mutexAcquired = instanceMutex.WaitOne(TimeSpan.Zero, false);
            }
            catch (AbandonedMutexException)
            {
                mutexAcquired = true;
            }

            if (!mutexAcquired)
            {
                MessageBox.Show(
                    "Microlux ERG-Connect уже запущено. Дождитесь завершения работы текущего экземпляра и повторите попытку.",
                    "Microlux ERG-Connect",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

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
        finally
        {
            if (mutexAcquired)
            {
                try
                {
                    instanceMutex.ReleaseMutex();
                }
                catch (ApplicationException)
                {
                    // Игнорируем попытки повторного освобождения.
                }
            }
        }
    }
}
