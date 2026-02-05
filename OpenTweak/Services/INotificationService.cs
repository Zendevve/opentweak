using System;

namespace OpenTweak.Services;

/// <summary>
/// Service for displaying user-facing notifications and feedback.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Shows a success message.
    /// </summary>
    void ShowSuccess(string message, string title = "Success");

    /// <summary>
    /// Shows an error message.
    /// </summary>
    void ShowError(string message, string title = "Error");

    /// <summary>
    /// Shows a warning message.
    /// </summary>
    void ShowWarning(string message, string title = "Warning");
}
