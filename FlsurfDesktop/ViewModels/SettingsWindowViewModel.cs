using FlsurfDesktop.Core.Services;
using FlsurfDesktop.Services;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace FlsurfDesktop.ViewModels
{
    // Модель для представления языка в выпадающем списке
    public record Language(string Name, string Code);

    public class SettingsWindowViewModel : ReactiveObject
    {
        private readonly AuthService _authService;
        private readonly IViewManager _viewManager;

        // --- Свойства для привязки к UI ---

        public List<Language> AvailableLanguages { get; }

        private Language _selectedLanguage;
        public Language SelectedLanguage
        {
            get => _selectedLanguage;
            set => this.RaiseAndSetIfChanged(ref _selectedLanguage, value);
        }

        private bool _notificationsEnabled;
        public bool NotificationsEnabled
        {
            get => _notificationsEnabled;
            set => this.RaiseAndSetIfChanged(ref _notificationsEnabled, value);
        }

        private bool _badgeCountEnabled;
        public bool BadgeCountEnabled
        {
            get => _badgeCountEnabled;
            set => this.RaiseAndSetIfChanged(ref _badgeCountEnabled, value);
        }

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            private set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        // --- Команды ---

        public ReactiveCommand<Unit, Unit> SaveSettingsCommand { get; }
        public ReactiveCommand<Unit, Unit> LogoutCommand { get; }

        public SettingsWindowViewModel(AuthService authService, IViewManager viewManager)
        {
            _authService = authService;
            _viewManager = viewManager;

            // --- Инициализация ---

            AvailableLanguages = new List<Language>
            {
                new("English", "en"),
                new("Русский", "ru"),
                new("Қазақ", "kk")
            };

            // Загружаем текущие настройки
            var currentUser = _authService.CurrentUserProfile;
            _selectedLanguage = AvailableLanguages.Find(l => l.Code == (currentUser?.PreferredLanguage ?? "ru")) ?? AvailableLanguages[1];

            // ВАЖНО: В вашем API (Client.cs) нет полей для этих настроек в UserProfile/UserEntity.
            // Я добавил заглушки. Вам нужно будет добавить эти поля в API.
            _notificationsEnabled = currentUser?.NotificationSettings?.DesktopNotificationsEnabled ?? true;
            _badgeCountEnabled = currentUser?.NotificationSettings?.DesktopBadgeCountEnabled ?? true;

            // --- Настройка команд ---

            SaveSettingsCommand = ReactiveCommand.CreateFromTask(SaveChanges);
            LogoutCommand = ReactiveCommand.Create(Logout);

            // При изменении языка, вызываем сервис локализации
            this.WhenAnyValue(x => x.SelectedLanguage)
                .Skip(1) // Пропускаем первоначальную установку значения
                .Subscribe(lang =>
                {
                    LocalizationService.SetCulture(lang.Code);
                    StatusMessage = "Язык изменится после перезапуска приложения.";
                });
        }

        private async Task SaveChanges()
        {
            StatusMessage = "Saving...";

            // ВАЖНО: Ваш сгенерированный API-клиент (IApiService) не имеет метода
            // для обновления настроек уведомлений. Метод UpdateUser не содержит этих полей.
            // Когда вы добавите такой метод в API и перегенерируете клиент,
            // вы сможете раскомментировать и использовать следующий код:

            /*
            var result = await _authService.UpdateNotificationSettingsAsync(new NotificationSettingsModel
            {
                DesktopNotificationsEnabled = this.NotificationsEnabled,
                DesktopBadgeCountEnabled = this.BadgeCountEnabled,
                PreferredLanguage = this.SelectedLanguage.Code
            });

            StatusMessage = result ? "Settings saved successfully." : "Failed to save settings.";
            */

            await Task.Delay(500); // Имитация сохранения
            StatusMessage = "Настройки уведомлений не могут быть сохранены (API не поддерживает).";
        }

        private void Logout()
        {
            _authService.LogoutAsync(); // Сбрасываем локальные данные аутентификации
            _viewManager.RestartApplication(); // Просим ViewManager перезапустить приложение
        }
    }
}