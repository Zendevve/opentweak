using System;
using System.Windows;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace OpenTweak.Services;

/// <summary>
/// Implementation of INotificationService using WPF UI SnackbarService.
/// </summary>
public class NotificationService : INotificationService
{
    private readonly ISnackbarService _snackbarService;

    public NotificationService(ISnackbarService snackbarService)
    {
        _snackbarService = snackbarService ?? throw new ArgumentNullException(nameof(snackbarService));
    }

    public void ShowSuccess(string message, string title = "Success")
    {
        Show(title, message, ControlAppearance.Success);
    }

    public void ShowError(string message, string title = "Error")
    {
        Show(title, message, ControlAppearance.Danger);
    }

    public void ShowWarning(string message, string title = "Warning")
    {
        Show(title, message, ControlAppearance.Caution);
    }

    private void Show(string title, string message, ControlAppearance appearance)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            _snackbarService.Show(
                title,
                message,
                appearance,
                new SymbolIcon(SymbolRegular.Info24),
                TimeSpan.FromSeconds(5)
            );
        });
    }
}
