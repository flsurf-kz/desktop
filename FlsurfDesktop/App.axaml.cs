using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FlsurfDesktop.ViewModels; // <-- Добавьте using для ваших ViewModel
using FlsurfDesktop.Views;      // <-- Добавьте using для ваших View
using FlsurfDesktop.Core.Services; // <-- Добавьте using для ваших сервисов
using Microsoft.Extensions.DependencyInjection; // <-- Важный using для DI
using System;

namespace FlsurfDesktop;

public partial class App : Application
{
    // 1. Создаём статическое свойство для хранения сервисов.
    // Оно должно быть public, чтобы ViewModel мог к нему обратиться.
    public static IServiceProvider Services { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 2. Создаём коллекцию сервисов
            var services = new ServiceCollection();

            // 3. Регистрируем все ваши зависимости
            ConfigureServices(services);

            // 4. Собираем провайдер сервисов
            Services = services.BuildServiceProvider();

            // 5. Создаём ГЛАВНУЮ ViewModel через контейнер
            var mainViewModel = Services.GetRequiredService<MainWindowViewModel>();

            // 6. Создаём окно и ПРИВЯЗЫВАЕМ к нему ViewModel
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel // <-- Вот ключевая связь!
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    // 7. Создаём отдельный метод для регистрации всех зависимостей
    private void ConfigureServices(IServiceCollection services)
    {
        // --- РЕГИСТРАЦИЯ СЕРВИСОВ ---
        // AddSingleton - создаётся один раз на всё время жизни приложения
        services.AddSingleton<AuthService>();
        services.AddSingleton<SessionService>();

        // --- РЕГИСТРАЦИЯ VIEWMODEL ---
        // AddTransient - создаётся новый экземпляр каждый раз, когда запрашивается
        services.AddTransient<LoginWindowViewModel>();
        services.AddTransient<SettingsWindowViewModel>();
        services.AddTransient<NotificationsViewModel>();
        services.AddTransient<FreelancerDashboardViewModel>();
        services.AddTransient<ClientDashboardViewModel>();

        // Главную ViewModel лучше как Singleton, т.к. она одна на всё приложение
        services.AddSingleton<MainWindowViewModel>();
    }
}