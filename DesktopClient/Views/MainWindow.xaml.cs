using System;
using System.ComponentModel;
using System.Windows;
using MicroluxErgConnect.Infrastructure;
using MicroluxErgConnect.ViewModels;
using Forms = System.Windows.Forms;
using Drawing = System.Drawing;

namespace MicroluxErgConnect.Views;

public partial class MainWindow : Window
{
    private readonly Forms.NotifyIcon _notifyIcon;
    private bool _isExitRequested;

    public MainWindow()
    {
        InitializeComponent();
        DataContext = AppServices.MainViewModel;
        var version = typeof(MainWindow).Assembly.GetName().Version?.ToString() ?? "1.0.0.0";
        Tag = version;
        _notifyIcon = new Forms.NotifyIcon
        {
            Icon = Drawing.SystemIcons.Application,
            Text = "Микролюкс ERG-Connect",
            Visible = true,
            ContextMenuStrip = new Forms.ContextMenuStrip()
        };
        _notifyIcon.ContextMenuStrip.Items.Add("Открыть", null, (_, _) => ShowFromTray());
        _notifyIcon.ContextMenuStrip.Items.Add("Выход", null, (_, _) => ExitApplication());
        _notifyIcon.DoubleClick += (_, _) => ShowFromTray();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (AppServices.Settings.Current.StartMinimized)
        {
            Hide();
        }
    }

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);
        if (WindowState == WindowState.Minimized && AppServices.Settings.Current.MinimizeToTray)
        {
            Hide();
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!_isExitRequested && AppServices.Settings.Current.MinimizeToTray)
        {
            e.Cancel = true;
            Hide();
            return;
        }
        base.OnClosing(e);
    }

    private void ShowFromTray()
    {
        Show();
        WindowState = WindowState.Normal;
        Activate();
    }

    private void ExitApplication()
    {
        _isExitRequested = true;
        _notifyIcon.Visible = false;
        Application.Current.Shutdown();
    }

    protected override void OnClosed(EventArgs e)
    {
        _notifyIcon.Dispose();
        base.OnClosed(e);
    }
}
