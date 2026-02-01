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
using Microsoft.Extensions.DependencyInjection;
using OpenTweak.Services;
using OpenTweak.ViewModels;
using Wpf.Ui;
using Wpf.Ui.Appearance;

namespace OpenTweak;

/// <summary>
/// Application entry point with WPFUI theming support and dependency injection.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Gets the service provider for resolving dependencies.
    /// </summary>
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Configure dependency injection
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        // Apply system theme (follows Windows dark/light mode)
        ApplicationThemeManager.ApplySystemTheme();
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Services - singletons for shared state
        services.AddSingleton<IDatabaseService>(_ => DatabaseService.Instance);
        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<IPCGWService, PCGWService>();
        services.AddSingleton<IGameScanner, GameScanner>();

        // TweakEngine depends on IBackupService
        services.AddSingleton<ITweakEngine, TweakEngine>();

        // ViewModels - transient for fresh instances per window
        services.AddTransient<MainViewModel>();
        services.AddTransient<GameDetailViewModel>();
    }
}
