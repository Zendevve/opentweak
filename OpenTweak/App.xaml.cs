// OpenTweak - PC Game Optimization Tool
// Copyright 2024-2025 OpenTweak Contributors
// Licensed under PolyForm Shield License 1.0.0
//
// This software is source-available and free to build for personal use.
// Pre-built binaries are available for purchase at: https://buymeacoffee.com/opentweak
// See LICENSE.md and docs/COMMERCIAL.md for full terms.
//
// NOTICE: Automated build services that distribute binaries are PROHIBITED.

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
