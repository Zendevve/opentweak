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

using Serilog;
using Polly;
using Polly.Extensions.Http;
using System.Net.Http;

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

        try
        {
            // Now create and show the main window (after services are ready)
            var mainWindow = new Views.MainWindow(Services.GetRequiredService<ISnackbarService>());

            mainWindow.Show();
        }
        catch (Exception ex)
        {
             MessageBox.Show(ex.ToString());
             throw;
        }
    }

    private static void ConfigureServices(IServiceCollection services)
    {
        // Logging
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("logs/log-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        services.AddLogging(lb => lb.AddSerilog());

        // UI Services
        services.AddSingleton<ISnackbarService, SnackbarService>();
        services.AddSingleton<INotificationService, NotificationService>();

        // Services - singletons for shared state
        services.AddSingleton<IDatabaseService>(_ => DatabaseService.Instance);
        services.AddSingleton<IBackupService, BackupService>();

        // Resilience Policy
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .OrResult(msg => msg.StatusCode == System.Net.HttpStatusCode.NotFound) // Handling PCGW weirdness
            .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        // Register HTTP Client with Polly
        services.AddHttpClient<IPCGWService, PCGWService>()
            .AddPolicyHandler(retryPolicy);

        // PCGW result caching to reduce API calls
        services.AddSingleton<PCGWCache>();

        services.AddSingleton<IGameScanner, GameScanner>();

        // TweakEngine depends on IBackupService
        services.AddSingleton<ITweakEngine, TweakEngine>();

        // ViewModels - transient for fresh instances per window
        services.AddTransient<MainViewModel>();
        services.AddTransient<GameDetailViewModel>();
    }
}
