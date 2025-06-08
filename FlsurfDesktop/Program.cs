// Program.cs - точка входа Avalonia-приложения с DI, Serilog и OIDC-авторизацией.
using Avalonia;
using FlsurfDesktop.Core.Services;
using FlsurfDesktop.Platform;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace FlsurfDesktop
{
    internal class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            // Собираем и стартуем Generic Host
            var host = CreateHostBuilder(args).Build();

            // Запускаем Avalonia с передачей DI-контейнера
            BuildAvaloniaApp(host).StartWithClassicDesktopLifetime(args);
        }

        /// <summary>Создаёт IHostBuilder: Serilog, DI, HttpClient, HostedServices.</summary>
        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                // Перенаправляем логи в Serilog (конфиг читается из appsettings.json)
                .UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration))
                .ConfigureServices((ctx, services) =>
                {
                    /* ---------- Core-слой ---------- */
                    services.AddSingleton<AuthService>();
                    services.AddSingleton<SessionService>();
                    services.AddHostedService<SessionTrackerService>();
                    services.AddSingleton<NotificationService>();

                    /* ---------- Http-клиент API ---------- */
                    services.AddHttpClient<ApiService>(client =>
                    {
                        client.BaseAddress = new Uri(ctx.Configuration["Api:BaseUrl"]);
                    })
                    // Подмешиваем заголовок Authorization ко всем запросам
                    .AddHttpMessageHandler(sp =>
                        new AuthHeaderHandler(() => sp.GetRequiredService<AuthService>().AccessToken));

                    /* ---------- Платформенные сервисы ---------- */
#if WINDOWS
                    services.AddSingleton<IScreenCaptureService, ScreenCaptureServiceWin>();
#else
                    // Временно заглушка – добавь реализацию для macOS/Linux позже
                    services.AddSingleton<IScreenCaptureService, StubScreenCaptureService>();
#endif
                });

        /// <summary>Строит AvaloniaApp и запускает Host.</summary>
        private static AppBuilder BuildAvaloniaApp(IHost host) =>
            AppBuilder.Configure<App>(() => new App(host.Services))
                      .UsePlatformDetect()
                      .LogToTrace()
                      .AfterSetup(_ => host.Start());
    }

    // ---------- Вспомогательные классы ----------

    /// <summary>DelegatingHandler, который вставляет Bearer-токен в каждый запрос.</summary>
    internal sealed class AuthHeaderHandler : DelegatingHandler
    {
        private readonly Func<string> _tokenProvider;

        public AuthHeaderHandler(Func<string> tokenProvider) => _tokenProvider = tokenProvider;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var token = _tokenProvider();
            if (!string.IsNullOrWhiteSpace(token))
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            return base.SendAsync(request, ct);
        }
    }

#if !WINDOWS
    /// <summary>Заглушка захвата экрана для macOS/Linux, пока не реализовано.</summary>
    internal sealed class StubScreenCaptureService : IScreenCaptureService
    {
        public Task<byte[]> CapturePrimaryScreenAsync() =>
            Task.FromResult(Array.Empty<byte>());
    }
#endif
}
