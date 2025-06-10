using Avalonia;
using Avalonia.ReactiveUI;
using FlsurfDesktop.Core.Services;
using FlsurfDesktop.Core.Models;
using FlsurfDesktop.ViewModels;
using FlsurfDesktop.RestClient;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FlsurfDesktop;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var host = CreateHostBuilder(args).Build();
        // Сохраняем ServiceProvider в статическое свойство для доступа в редких случаях,
        // но стараемся его не использовать напрямую.
        App.Services = host.Services;

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
            .UseSerilog((context, configuration) =>
                configuration.ReadFrom.Configuration(context.Configuration).WriteTo.Console())
            .ConfigureServices((hostContext, services) =>
            {
                // --- СЕРВИСЫ ЯДРА (Core Services) ---
                // Singleton - один экземпляр на все приложение
                services.AddSingleton<AuthService>();
                services.AddSingleton<SessionService>();

                // --- HTTP-КЛИЕНТ И API ---
                // Регистрируем обработчик, который будет добавлять токен авторизации
                services.AddTransient<AuthHeaderHandler>();

                // Регистрируем сгенерированный NSwag клиент
                services.AddHttpClient<IApiService, ApiService>(client =>
                {
                    // URL вашего API (в идеале из appsettings.json)
                    client.BaseAddress = new Uri("http://localhost:8000");
                })
                .AddHttpMessageHandler<AuthHeaderHandler>();

                // --- СЕРВИСЫ ПРИЛОЖЕНИЯ ---
                services.AddSingleton<IViewManager, ViewManager>();
                services.AddSingleton<IScreenCaptureService, StubScreenCaptureService>(); // Заглушка, замените на реализацию

                // --- VIEWMODELS ---
                // Окна и страницы, которые могут создаваться много раз
                services.AddTransient<LoginWindowViewModel>();
                services.AddTransient<SecretPhraseWindowViewModel>();
                services.AddTransient<SettingsWindowViewModel>();
                services.AddTransient<FreelancerDashboardViewModel>();
                services.AddTransient<ClientDashboardViewModel>();
                services.AddTransient<NotificationsViewModel>();
                services.AddTransient<ContractDetailViewModel>();
                services.AddTransient<SessionDetailViewModel>();

                // Главная ViewModel - одна на все приложение
                services.AddSingleton<MainWindowViewModel>();
            });

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace()
            .UseReactiveUI();
}


/// <summary>
/// Вспомогательный класс для автоматического добавления Bearer токена в каждый HTTP-запрос к API.
/// </summary>
public class AuthHeaderHandler : DelegatingHandler
{
    private readonly IServiceProvider _services;

    public AuthHeaderHandler(IServiceProvider services)
    {
        _services = services;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Используем Scope, чтобы не создавать "захваченную" зависимость в Singleton-обработчике
        using (var scope = _services.CreateScope())
        {
            var authService = scope.ServiceProvider.GetRequiredService<AuthService>();
            if (!string.IsNullOrWhiteSpace(authService.AccessToken))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authService.AccessToken);
            }
        }

        return base.SendAsync(request, cancellationToken);
    }
}