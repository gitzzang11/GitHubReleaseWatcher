using System.Drawing;
using Forms = System.Windows.Forms;

namespace GitHubReleaseWatcher.Services;

public sealed class TrayService : IDisposable
{
    private readonly Forms.NotifyIcon _notifyIcon;

    public TrayService()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add("열기", null, (_, _) => OpenRequested?.Invoke());
        menu.Items.Add("지금 업데이트 확인", null, (_, _) => CheckRequested?.Invoke());
        menu.Items.Add(new Forms.ToolStripSeparator());
        menu.Items.Add("종료", null, (_, _) => ExitRequested?.Invoke());

        _notifyIcon = new Forms.NotifyIcon
        {
            Text = "GitHub Release Watcher",
            Icon = CreateIcon(),
            ContextMenuStrip = menu,
            Visible = true
        };
        _notifyIcon.DoubleClick += (_, _) => OpenRequested?.Invoke();
    }

    public event Action? OpenRequested;
    public event Action? CheckRequested;
    public event Action? ExitRequested;

    public void ShowBalloon(string text) => _notifyIcon.ShowBalloonTip(
        2500, "GitHub Release Watcher", text, Forms.ToolTipIcon.Info);

    private static Icon CreateIcon()
    {
        var executable = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(executable))
        {
            using var associated = Icon.ExtractAssociatedIcon(executable);
            if (associated is not null)
            {
                return (Icon)associated.Clone();
            }
        }

        return (Icon)SystemIcons.Application.Clone();
    }

    public void Dispose()
    {
        _notifyIcon.Visible = false;
        _notifyIcon.Icon?.Dispose();
        _notifyIcon.ContextMenuStrip?.Dispose();
        _notifyIcon.Dispose();
    }
}
