using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Appearance;

namespace OpenTweak;

/// <summary>
/// Application entry point with WPFUI theming support.
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Apply system theme (follows Windows dark/light mode)
        ApplicationThemeManager.ApplySystemTheme();
    }
}
