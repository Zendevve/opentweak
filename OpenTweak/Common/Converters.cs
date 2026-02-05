// OpenTweak - PC Game Optimization Tool
// Copyright 2024-2025 OpenTweak Contributors
// Licensed under PolyForm Shield License 1.0.0
// See LICENSE.md for full terms.

using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using OpenTweak.ViewModels;
using Wpf.Ui.Controls;

namespace OpenTweak.Common;

/// <summary>
/// XAML value converters used throughout the application.
/// Centralized here for reusability and easier testing.
/// </summary>
public static class Converters
{
    // Singleton instances for XAML resource dictionaries
    public static InverseBoolConverter InverseBool { get; } = new();
    public static NullToVisibilityConverter NullToVisibility { get; } = new();
    public static NotNullToVisibilityConverter NotNullToVisibility { get; } = new();
    public static ZeroToVisibilityConverter ZeroToVisibility { get; } = new();
    public static ViewModeToAppearanceConverter ViewModeToAppearance { get; } = new();
    public static ViewModeToVisibilityConverter ViewModeToVisibility { get; } = new();
    public static SelectionToBrushConverter SelectionToBrush { get; } = new();
}

/// <summary>
/// Converter that inverts a boolean value.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }
}

/// <summary>
/// Converter that returns Visible when value is null.
/// </summary>
public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value == null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("NullToVisibilityConverter cannot convert back.");
    }
}

/// <summary>
/// Converter that returns Visible when value is not null.
/// </summary>
public class NotNullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value != null ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("NotNullToVisibilityConverter cannot convert back.");
    }
}

/// <summary>
/// Converter that returns Visible when count is zero.
/// </summary>
public class ZeroToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("ZeroToVisibilityConverter cannot convert back.");
    }
}

/// <summary>
/// Converter that returns the appropriate WPFUI ControlAppearance based on view mode.
/// Used for styling view toggle buttons.
/// </summary>
public class ViewModeToAppearanceConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ViewMode currentMode && parameter is string targetMode)
        {
            var target = Enum.Parse<ViewMode>(targetMode);
            return currentMode == target ? ControlAppearance.Primary : ControlAppearance.Secondary;
        }
        return ControlAppearance.Secondary;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("ViewModeToAppearanceConverter cannot convert back.");
    }
}

/// <summary>
/// Converter that returns Visible for the matching view mode.
/// Used to show/hide List vs Grid views.
/// </summary>
public class ViewModeToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is ViewMode currentMode && parameter is string targetMode)
        {
            var target = Enum.Parse<ViewMode>(targetMode);
            return currentMode == target ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("ViewModeToVisibilityConverter cannot convert back.");
    }
}

/// <summary>
/// Converter that returns a brush based on selection state.
/// Returns a subtle highlight when selected, transparent otherwise.
/// </summary>
public class SelectionToBrushConverter : IValueConverter
{
    private static readonly Brush SelectedBrush = new SolidColorBrush(Color.FromArgb(32, 128, 128, 128));
    private static readonly Brush UnselectedBrush = new SolidColorBrush(Colors.Transparent);

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool isSelected && isSelected)
        {
            return SelectedBrush;
        }
        return UnselectedBrush;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotSupportedException("SelectionToBrushConverter cannot convert back.");
    }
}
