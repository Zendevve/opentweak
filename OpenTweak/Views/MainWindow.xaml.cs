// OpenTweak - PC Game Optimization Tool
// Copyright 2024-2025 OpenTweak Contributors
// Licensed under PolyForm Shield License 1.0.0
// See LICENSE.md for full terms.

using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Extensions.DependencyInjection;
using Wpf.Ui.Controls;
using OpenTweak.ViewModels;
using OpenTweak.Models;

namespace OpenTweak.Views;

/// <summary>
/// Main window with WPFUI FluentWindow styling.
/// </summary>
public partial class MainWindow : FluentWindow
{
    private Storyboard? _slideInStoryboard;
    private Storyboard? _slideOutStoryboard;
    private Storyboard? _fadeInStoryboard;
    private Storyboard? _fadeOutStoryboard;

    public MainWindow()
    {
        InitializeComponent();

        // Resolve ViewModel from DI container
        DataContext = App.Services.GetRequiredService<MainViewModel>();

        InitializeAnimations();
        DataContextChanged += OnDataContextChanged;
    }

    private void InitializeAnimations()
    {
        // Slide-in animation for the panel
        _slideInStoryboard = new Storyboard();
        var slideInAnimation = new DoubleAnimation
        {
            From = 520,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(300),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
        };
        Storyboard.SetTarget(slideInAnimation, SlideOverPanel);
        Storyboard.SetTargetProperty(slideInAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
        _slideInStoryboard.Children.Add(slideInAnimation);

        // Slide-out animation for the panel
        _slideOutStoryboard = new Storyboard();
        var slideOutAnimation = new DoubleAnimation
        {
            From = 0,
            To = 520,
            Duration = TimeSpan.FromMilliseconds(250),
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        };
        Storyboard.SetTarget(slideOutAnimation, SlideOverPanel);
        Storyboard.SetTargetProperty(slideOutAnimation, new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
        _slideOutStoryboard.Children.Add(slideOutAnimation);

        // Fade-in animation for the backdrop
        _fadeInStoryboard = new Storyboard();
        var fadeInAnimation = new DoubleAnimation
        {
            From = 0,
            To = 1,
            Duration = TimeSpan.FromMilliseconds(200)
        };
        Storyboard.SetTarget(fadeInAnimation, SlideOverBackdrop);
        Storyboard.SetTargetProperty(fadeInAnimation, new PropertyPath("Opacity"));
        _fadeInStoryboard.Children.Add(fadeInAnimation);

        // Fade-out animation for the backdrop
        _fadeOutStoryboard = new Storyboard();
        var fadeOutAnimation = new DoubleAnimation
        {
            From = 1,
            To = 0,
            Duration = TimeSpan.FromMilliseconds(200)
        };
        Storyboard.SetTarget(fadeOutAnimation, SlideOverBackdrop);
        Storyboard.SetTargetProperty(fadeOutAnimation, new PropertyPath("Opacity"));
        _fadeOutStoryboard.Children.Add(fadeOutAnimation);
        _fadeOutStoryboard.Completed += (s, e) => SlideOverBackdrop.Visibility = Visibility.Collapsed;
    }

    private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (e.OldValue is MainViewModel oldVm)
        {
            oldVm.PropertyChanged -= OnViewModelPropertyChanged;
        }

        if (e.NewValue is MainViewModel newVm)
        {
            newVm.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsSlideOverOpen))
        {
            var vm = (MainViewModel)DataContext;
            if (vm.IsSlideOverOpen)
            {
                OpenSlideOver();
            }
            else
            {
                CloseSlideOver();
            }
        }
    }

    private void OpenSlideOver()
    {
        SlideOverBackdrop.Visibility = Visibility.Visible;
        _fadeInStoryboard?.Begin();
        _slideInStoryboard?.Begin();
    }

    private void CloseSlideOver()
    {
        _slideOutStoryboard?.Begin();
        _fadeOutStoryboard?.Begin();
    }

    private void OnGameDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is ListBoxItem item && item.DataContext is Game game)
        {
            if (DataContext is MainViewModel vm)
            {
                _ = vm.OpenGameDetailsAsync(game);
            }
        }
    }

    private void OnGameCardClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is Border border && border.DataContext is Game game)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SelectedGame = game;
                _ = vm.OpenGameDetailsAsync(game);
            }
        }
    }

    private void OnBackdropClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.CloseSlideOver();
        }
    }
    private void OnHyperlinkNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
    {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
        e.Handled = true;
    }
}
