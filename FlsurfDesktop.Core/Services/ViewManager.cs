using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;
using System;
using FlsurfDesktop.ViewModels; 
namespace FlsurfDesktop.Services;

public class ViewManager : IViewManager
{
    private readonly IServiceProvider _services;
    private Window? _loginWindow;

    public ViewManager(IServiceProvider services)
    {
        _services = services;
    }

    public Window ShowLoginWindow()
    {
        var vm = _services.GetRequiredService<LoginWindowViewModel>();
        var window = new LoginWindow { DataContext = vm };
        _loginWindow = window;
        return window;
    }

    public void ShowMainWindowAndCloseLogin()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

        var vm = _services.GetRequiredService<MainWindowViewModel>();
        var newMainWindow = new MainWindow { DataContext = vm };

        desktop.MainWindow = newMainWindow;
        newMainWindow.Show();

        _loginWindow?.Close();
        _loginWindow = null;
    }

    public bool ShowSecretPhraseDialog(Guid userId)
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return false;

        var vm = _services.GetRequiredService<SecretPhraseWindowViewModel>();
        vm.Initialize(userId);

        var window = new SecretPhraseWindow { DataContext = vm };
        window.ShowDialog(desktop.MainWindow ?? _loginWindow);

        return vm.IsVerified;
    }

    public void ShowSettingsWindow()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

        var vm = _services.GetRequiredService<SettingsWindowViewModel>();
        var window = new SettingsWindow { DataContext = vm };
        window.ShowDialog(desktop.MainWindow);
    }

    public void RestartApplication()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop) return;

        var oldWindow = desktop.MainWindow;
        desktop.MainWindow = ShowLoginWindow();
        desktop.MainWindow.Show();
        oldWindow?.Close();
    }
}