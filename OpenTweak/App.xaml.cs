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
        System.IO.File.AppendAllText("debug_log.txt", "Startup: OnStartup called\n");

        // Set up global exception handling to help diagnose crashes
        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            System.Windows.MessageBox.Show(
                $"A fatal error occurred:\n\n{ex?.Message}\n\n{ex?.StackTrace}",
                "OpenTweak Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        };

        DispatcherUnhandledException += (s, args) =>
        {
            System.Windows.MessageBox.Show(
                $"An error occurred:\n\n{args.Exception.Message}\n\n{args.Exception.StackTrace}",
                "OpenTweak Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        base.OnStartup(e);

        // Configure dependency injection BEFORE creating any windows
        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        // Apply system theme (follows Windows dark/light mode)
        ApplicationThemeManager.ApplySystemTheme();

        // Debug logging
        System.IO.File.AppendAllText("debug_log.txt", "Startup: Services Configured\n");

        try
        {
            // Now create and show the main window (after services are ready)
            var mainWindow = new Views.MainWindow();
            System.IO.File.AppendAllText("debug_log.txt", "Startup: MainWindow Created\n");

            mainWindow.Show();
            System.IO.File.AppendAllText("debug_log.txt", "Startup: MainWindow Shown\n");
        }
        catch (Exception ex)
        {
             System.IO.File.AppendAllText("debug_log.txt", $"Startup Error: {ex}\n");
             MessageBox.Show(ex.ToString());
             throw;
        }
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
