using Avalonia.Controls;

namespace FEx.Avaloniax.Desktop;

public class FExDesktopWindow : Window
{
    public FExTrayIcon TrayIcon { get; set; }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (TrayIcon is not null)
        {
            e.Cancel = true;
            Hide();
            TrayIcon.Show();
        }

        base.OnClosing(e);
    }
}
