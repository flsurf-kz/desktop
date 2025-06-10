using Avalonia.Media.Imaging;
using FlsurfDesktop.RestClient;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace FlsurfDesktop.ViewModels
{
    public class SessionDetailViewModel : ReactiveObject
    {
        private readonly IApiService _api;

        // Свойства для данных, которые будут загружены
        private WorkSessionEntity? _sessionDetails;
        public WorkSessionEntity? SessionDetails
        {
            get => _sessionDetails;
            private set => this.RaiseAndSetIfChanged(ref _sessionDetails, value);
        }

        private bool _isLoading = true;
        public bool IsLoading
        {
            get => _isLoading;
            private set => this.RaiseAndSetIfChanged(ref _isLoading, value);
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            private set => this.RaiseAndSetIfChanged(ref _errorMessage, value);
        }

        // Коллекция скриншотов
        public ObservableCollection<Bitmap> ScreenshotThumbnails { get; } = new();

        // Команда для загрузки данных
        public ReactiveCommand<Guid, WorkSessionEntity> LoadSessionDetailCommand { get; }

        public SessionDetailViewModel(IApiService api)
        {
            _api = api;

            // 1. Команда для загрузки деталей сессии. 
            // Она принимает Guid сессии и возвращает WorkSessionEntity.
            LoadSessionDetailCommand = ReactiveCommand.CreateFromTask<Guid, WorkSessionEntity>(
                sessionId => _api.GetSessionAsync(sessionId)
            );

            // 2. Подписываемся на результат выполнения команды.
            LoadSessionDetailCommand.Subscribe(session =>
            {
                SessionDetails = session;
                // Запускаем загрузку изображений после получения информации о сессии
                _ = LoadScreenshotsAsync(session.Files);
            });

            // 3. Обработка ошибок при загрузке
            LoadSessionDetailCommand.ThrownExceptions.Subscribe(ex =>
            {
                ErrorMessage = $"Failed to load session details: {ex.Message}";
            });

            // 4. Управление индикатором загрузки
            LoadSessionDetailCommand.IsExecuting.Subscribe(isLoading =>
            {
                IsLoading = isLoading;
            });
        }

        // 5. Публичный метод для инициализации ViewModel с runtime-параметром.
        public async Task InitializeAsync(Guid sessionId)
        {
            ErrorMessage = string.Empty;
            await LoadSessionDetailCommand.Execute(sessionId);
        }

        private async Task LoadScreenshotsAsync(System.Collections.Generic.ICollection<FileEntity> files)
        {
            ScreenshotThumbnails.Clear();
            if (files == null) return;

            foreach (var file in files)
            {
                try
                {
                    // ВНИМАНИЕ: Метод DownloadFileAsync в вашем API возвращает Task, а не данные файла.
                    // Это значит, что он не возвращает байты картинки.
                    // ПРАВИЛЬНЫЙ ПУТЬ: Ваш API должен иметь эндпоинт, который по file.Id или file.FilePath
                    // возвращает сам файл (byte[] или Stream).

                    // ЗАГЛУШКА: Поскольку текущий API не позволяет скачать файл,
                    // мы симулируем загрузку и показываем плейсхолдер.
                    // Когда вы исправите API, здесь будет реальный код загрузки.

                    // var fileData = await _api.DownloadFileAsBytesAsync(file.Id); // <- Примерный будущий метод
                    // using var ms = new MemoryStream(fileData);
                    // var bmp = Bitmap.DecodeToWidth(ms, 200);
                    // ScreenshotThumbnails.Add(bmp);

                    await Task.Delay(200); // Симуляция загрузки
                }
                catch (Exception ex)
                {
                    // Обработка ошибки загрузки одного скриншота
                }
            }
        }
    }
}