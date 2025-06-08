using System;
using System.Threading;
using System.Threading.Tasks;
using FlsurfDesktop.Core.Models;
using FlsurfDesktop.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FlsurfDesktop.Core.Services
{
    public class SessionService
    {
        private readonly ApiService _api;
        private readonly IScreenCaptureService _screenCapture;
        private CancellationTokenSource? _cts;
        private Guid _currentContractId;
        private DateTimeOffset _sessionStart;
        private decimal _earnedSoFar = 0m;
        private Timer _randomTimer;

        public bool IsActive { get; private set; } = false;

        /// <summary>Вызывается, когда сессия стартовала.</summary>
        public event Action<Guid, DateTimeOffset>? SessionStarted;

        /// <summary>Вызывается, когда сессия завершилась: контрактId, время окончания, заработано всего.</summary>
        public event Action<Guid, DateTimeOffset, decimal>? SessionEnded;

        /// <summary>Периодический тик (каждые 1 минуту) – возвращает сколько прошло и сколько заработано.</summary>
        public event Action<TimeSpan, decimal>? SessionPeriodicTick;

        public SessionService(ApiService api, IScreenCaptureService screenCapture)
        {
            _api = api;
            _screenCapture = screenCapture;
        }

        public async Task StartSessionAsync(Guid contractId)
        {
            if (IsActive) return;

            _currentContractId = contractId;
            _sessionStart = DateTimeOffset.UtcNow;
            IsActive = true;
            _earnedSoFar = 0m;

            // Нотифицируем внешний мир
            SessionStarted?.Invoke(contractId, _sessionStart);

            // Отправляем на бекенд команду START
            await _api.Client.StartSession(new StartWorkSessionCommand { ContractId = contractId });

            // Запускаем таймер случайных скриншотов в течение часа
            _cts = new CancellationTokenSource();
            _ = RunScreenshotLoopAsync(_cts.Token);

            // Запускаем периодический тик (каждую минуту), чтобы считать earnedSoFar
            _randomTimer = new Timer(_ =>
            {
                var elapsed = DateTimeOffset.UtcNow - _sessionStart;
                // Зарплата = elapsed.TotalHours * CostPerHour (надо получить CostPerHour)
                // Но мы можем запрашивать контракт (или сохранять из фмд) при старте
                // Для простоты пусть ApiService хранит CostPerHour
                var costPerHour = _api.CostPerHour(_currentContractId);
                _earnedSoFar = (decimal)elapsed.TotalHours * costPerHour;
                SessionPeriodicTick?.Invoke(elapsed, _earnedSoFar);
            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
        }

        public async Task StopSessionAsync()
        {
            if (!IsActive) return;

            IsActive = false;
            _cts?.Cancel();
            _randomTimer?.Dispose();

            var sessionEnd = DateTimeOffset.UtcNow;
            // Отправляем команду END на бекенд
            await _api.Client.EndSession(new EndWorkSessionCommand { ContractId = _currentContractId });

            // Предположим, что бекенд вернёт итоговую зарплату (но если нет, мы уже вычислили _earnedSoFar на последнем тике)
            decimal earnedTotal = _earnedSoFar;
            SessionEnded?.Invoke(_currentContractId, sessionEnd, earnedTotal);
        }

        private async Task RunScreenshotLoopAsync(CancellationToken token)
        {
            var rnd = new Random();
            while (!token.IsCancellationRequested)
            {
                // Ждём случайное время: 1–60 минут
                int minutesToNext = rnd.Next(1, 61);
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(minutesToNext), token);
                }
                catch (TaskCanceledException) { break; }

                if (token.IsCancellationRequested) break;

                // Делаем скриншот
                var pngBytes = await _screenCapture.CapturePrimaryScreenAsync();

                // Здесь можно запаковать PNG, добавить в multipart/form-data и отправить на бекенд:
                using var ms = new System.IO.MemoryStream(pngBytes);
                var fileParam = new FileParameter(ms, $"{Guid.NewGuid()}.png", "image/png");
                await _api.Client.UploadFile(fileParam);

                // Также можно отправить DTO WorkSessionFile, прикрепив его к _currentContractId и текущему времени.
            }
        }
    }
}
