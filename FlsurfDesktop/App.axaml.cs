using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using FlsurfDesktop.ViewModels; // <-- �������� using ��� ����� ViewModel
using FlsurfDesktop.Views;      // <-- �������� using ��� ����� View
using FlsurfDesktop.Core.Services; // <-- �������� using ��� ����� ��������
using Microsoft.Extensions.DependencyInjection; // <-- ������ using ��� DI
using System;

namespace FlsurfDesktop;

public partial class App : Application
{
    // 1. ������ ����������� �������� ��� �������� ��������.
    // ��� ������ ���� public, ����� ViewModel ��� � ���� ����������.
    public static IServiceProvider Services { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 2. ������ ��������� ��������
            var services = new ServiceCollection();

            // 3. ������������ ��� ���� �����������
            ConfigureServices(services);

            // 4. �������� ��������� ��������
            Services = services.BuildServiceProvider();

            // 5. ������ ������� ViewModel ����� ���������
            var mainViewModel = Services.GetRequiredService<MainWindowViewModel>();

            // 6. ������ ���� � ����������� � ���� ViewModel
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainViewModel // <-- ��� �������� �����!
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    // 7. ������ ��������� ����� ��� ����������� ���� ������������
    private void ConfigureServices(IServiceCollection services)
    {
        // --- ����������� �������� ---
        // AddSingleton - �������� ���� ��� �� �� ����� ����� ����������
        services.AddSingleton<AuthService>();
        services.AddSingleton<SessionService>();

        // --- ����������� VIEWMODEL ---
        // AddTransient - �������� ����� ��������� ������ ���, ����� �������������
        services.AddTransient<LoginWindowViewModel>();
        services.AddTransient<SettingsWindowViewModel>();
        services.AddTransient<NotificationsViewModel>();
        services.AddTransient<FreelancerDashboardViewModel>();
        services.AddTransient<ClientDashboardViewModel>();

        // ������� ViewModel ����� ��� Singleton, �.�. ��� ���� �� �� ����������
        services.AddSingleton<MainWindowViewModel>();
    }
}