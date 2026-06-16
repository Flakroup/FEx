using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using System;

namespace FEx.Avaloniax.Desktop;

public class FExTrayIcon
{
    private readonly string _toolTipText;
    private TrayIcon _trayIcon;

    public FExTrayIcon(string toolTipText)
    {
        _toolTipText = toolTipText;
    }

    public static void ExitApplication()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnMainWindowClose;
            desktop.MainWindow?.Close();
            desktop.Shutdown();
        }
    }

    public void Initialize(WindowIcon icon)
    {
        _trayIcon = new()
        {
            Icon = icon,
            ToolTipText = _toolTipText,
            IsVisible = false,
            Menu = CreateMenu()
        };

        _trayIcon.Clicked += OnTrayIconClicked;

        var icons = new TrayIcons
        {
            _trayIcon
        };

        TrayIcon.SetIcons(Application.Current!, icons);
    }

    public void Show()
    {
        _trayIcon?.IsVisible = true;
    }

    public void Hide()
    {
        _trayIcon?.IsVisible = false;
    }

    public void ShowMainWindow()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainWindow = desktop.MainWindow;

            if (mainWindow is not null)
            {
                mainWindow.Show();
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Activate();
            }
        }

        Hide();
    }

    private void OnTrayIconClicked(object sender, EventArgs e) => ShowMainWindow();

    private NativeMenu CreateMenu()
    {
        var showItem = new NativeMenuItem("Show");
        showItem.Click += (_, _) => ShowMainWindow();

        var exitItem = new NativeMenuItem("Exit");
        exitItem.Click += (_, _) => ExitApplication();

        var menu = new NativeMenu();
        menu.Items.Add(showItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(exitItem);

        return menu;
    }
}