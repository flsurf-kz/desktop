using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FlsurfDesktop.RestClient;
using Microsoft.Extensions.Logging;

namespace FlsurfDesktop.Core.Services;

public class SessionService
{
    private readonly IApiService _api;
    private readonly IScreenCaptureService _screenCapture;
    private readonly ILogger<SessionService> _logger;
    private CancellationTokenSource? _cts;
    private Timer? _ticker;

    public bool IsActive { get; private set; }
    public Guid CurrentContractId { get; private set; }
    public TimeSpan Elapsed { get; private set; }
    public decimal EarnedSoFar { get; private set; }
    private decimal _costPerHour;

    public event Action? SessionStateChanged;

    public SessionService(IApiService api, IScreenCaptureService screenCapture, ILogger<SessionService> logger)
    {
        _api = api;
        _screenCapture = screenCapture;
        _logger = logger;
    }

    public async Task StartSessionAsync(Guid contractId)
    {
        if (IsActive) return;

        _logger.LogInformation("Starting session for contract {ContractId}", contractId);

        try
        {
            var contract = await _api.GetContractAsync(contractId);
            _costPerHour = (decimal)contract.CostPerHour.Amount;

            // TODO: API должен вернуть ID сессии после старта
            // var sessionResult = await _api.StartSessionAsync(new StartWorkSessionCommand { ContractId = contractId });
            // var sessionId = Guid.Parse(sessionResult.Id);

            IsActive = true;
            CurrentContractId = contractId;
            EarnedSoFar = 0;
            Elapsed = TimeSpan.Zero;

            _cts = new CancellationTokenSource();

            // Запускаем тикер для обновления времени и заработка
            var startTime = DateTime.UtcNow;
            _ticker = new Timer(_ =>
            {
                Elapsed = DateTime.UtcNow - startTime;
                EarnedSoFar = (decimal)Elapsed.TotalHours * _costPerHour;
                SessionStateChanged?.Invoke();
            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));

            _ = RunScreenshotLoopAsync(_cts.Token);
            SessionStateChanged?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to start session for contract {ContractId}", contractId);
        }
    }

    public async Task StopSessionAsync()
    {
        if (!IsActive) return;
        _logger.LogInformation("Stopping session for contract {ContractId}", CurrentContractId);

        _cts?.Cancel();
        _cts?.Dispose();
        _ticker?.Dispose();

        try
        {
            // await _api.EndSessionAsync(new EndWorkSessionCommand { SessionId = ... });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to properly end session on server.");
        }

        IsActive = false;
        SessionStateChanged?.Invoke();
    }

    private async Task RunScreenshotLoopAsync(CancellationToken token)
    {
        var random = new Random();
        while (!token.IsCancellationRequested)
        {
            try
            {
                var delayMinutes = random.Next(1, 10);
                await Task.Delay(TimeSpan.FromMinutes(delayMinutes), token);

                _logger.LogInformation("Capturing screenshot for contract {ContractId}", CurrentContractId);
                byte[] screenshotBytes = await _screenCapture.CapturePrimaryScreenAsync();

                if (screenshotBytes.Length > 0)
                {
                    using var ms = new MemoryStream(screenshotBytes);
                    var fileParam = new FileParameter(ms, $"{Guid.NewGuid()}.png", "image/png");
                    // TODO: Привязать загруженный файл к рабочей сессии
                    // var fileEntity = await _api.UploadFileAsync(fileParam);
                    // await _api.AttachFileToSessionAsync(sessionId, fileEntity.Id);
                }
            }
            catch (TaskCanceledException) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Screenshot loop failed."); }
        }
    }
}