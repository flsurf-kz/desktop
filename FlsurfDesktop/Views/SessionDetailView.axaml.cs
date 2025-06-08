using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using FlsurfDesktop.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using Avalonia.Media.Imaging;

namespace FlsurfDesktop.ViewModels
{
    public class SessionDetailViewModel : ReactiveObject
    {
        private readonly ApiService _api;
        private Guid _sessionId;

        public DateTimeOffset StartDateLocal { get; private set; }
        public DateTimeOffset? EndDateLocal { get; private set; }
        public string DurationInfo { get; private set; } = "";
        public string Status { get; private set; } = "";
        public string FreelancerComment { get; private set; } = "";
        public string ClientComment { get; private set; } = "";

        public ObservableCollection<Bitmap> ScreenshotThumbnails { get; } = new();

        public ReactiveCommand<Unit, Unit> CloseCommand { get; }
        public Action? CloseWindow { get; set; }

        public SessionDetailViewModel(Guid sessionId)
        {
            _sessionId = sessionId;
            _api = App.Services.GetRequiredService<ApiService>();

            CloseCommand = ReactiveCommand.Create(() => CloseWindow?.Invoke());
            _ = LoadSessionDetailAsync();
        }

        private async Task LoadSessionDetailAsync()
        {
            var detail = await _api.Client.GetSession(_sessionId);
            StartDateLocal = detail.StartDate.ToLocalTime();
            EndDateLocal = detail.EndDate?.ToLocalTime();
            Status = detail.Status.ToString();
            FreelancerComment = detail.Comment ?? "";
            ClientComment = detail.ClientComment ?? "";

            var duration = EndDateLocal.HasValue
                ? EndDateLocal.Value - StartDateLocal
                : DateTimeOffset.Now - StartDateLocal;
            DurationInfo = $"Duration: {duration:h\\:mm}";
            this.RaisePropertyChanged(nameof(StartDateLocal));
            this.RaisePropertyChanged(nameof(EndDateLocal));
            this.RaisePropertyChanged(nameof(DurationInfo));
            this.RaisePropertyChanged(nameof(Status));
            this.RaisePropertyChanged(nameof(FreelancerComment));
            this.RaisePropertyChanged(nameof(ClientComment));

            // Скачиваем все скриншоты для этой сессии:
            foreach (var file in detail.Files)
            {
                // API у вас отдаёт FileEntity с FilePath → вызываем DownloadFile
                using var ms = new MemoryStream();
                await _api.Client.DownloadFile(file.Id); // пусть API кладёт байты в поток
                ms.Seek(0, SeekOrigin.Begin);
                var bmp = Bitmap.DecodeToWidth(ms, 400);
                ScreenshotThumbnails.Add(bmp);
            }
            this.RaisePropertyChanged(nameof(ScreenshotThumbnails));
        }
    }
}
