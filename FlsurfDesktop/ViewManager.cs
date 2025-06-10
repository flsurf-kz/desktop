using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using FlsurfDesktop.ViewModels;
using FlsurfDesktop.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
namespace FlsurfDesktop.Services;

using Avalonia.Controls;
using System;

/// <summary>
/// Определяет контракт для сервиса, который управляет навигацией и отображением окон (View).
/// ViewModel'и используют этот интерфейс, чтобы запрашивать открытие окон, не зная об их конкретных типах.
/// </summary>
public interface IViewManager
{
    /// <summary>
    /// Создает и показывает главное окно входа в приложение.
    /// </summary>
    /// <returns>Экземпляр окна входа для установки в качестве MainWindow.</returns>
    Window ShowLoginWindow();

    /// <summary>
    /// Показывает главное окно приложения и закрывает окно входа.
    /// </summary>
    void ShowMainWindowAndCloseLogin();

    /// <summary>
    /// Открывает окно настроек как диалог.
    /// </summary>
    void ShowSettingsWindow();

    /// <summary>
    /// Открывает модальное окно для верификации секретной фразы.
    /// </summary>
    /// <param name="userId">ID пользователя для верификации.</param>
    /// <returns>True, если верификация прошла успешно; иначе false.</returns>
    bool ShowSecretPhraseDialog(Guid userId);

    /// <summary>
    /// Перезапускает приложение, возвращая пользователя к окну входа.
    /// Необходимо для таких действий, как выход из системы или смена языка.
    /// </summary>
    void RestartApplication();
}

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