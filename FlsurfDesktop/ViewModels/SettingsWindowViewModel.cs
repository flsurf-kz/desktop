using ReactiveUI;
using System;
using System.Reactive;
using FlsurfDesktop.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FlsurfDesktop.ViewModels
{
    public class SettingsWindowViewModel : ReactiveObject
    {
        private readonly AuthService _authService;

        private string _selectedLanguage;
        public string SelectedLanguage
        {
            get => _selectedLanguage;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedLanguage, value);
                // Переключаем ResourceDictionary
                LocalizationService.SetCulture(value);
            }
        }

        private bool _notificationsEnabled;
        public bool NotificationsEnabled
        {
            get => _notificationsEnabled;
            set
            {
                this.RaiseAndSetIfChanged(ref _notificationsEnabled, value);
                // Сохраняем в профиль пользователя
                _authService.CurrentUserProfile.NotificationSettings.DesktopNotificationsEnabled = value;
                _ = _authService.UpdateNotificationSettingsAsync();
            }
        }

        private bool _badgeCountEnabled;
        public bool BadgeCountEnabled
        {
            get => _badgeCountEnabled;
            set
            {
                this.RaiseAndSetIfChanged(ref _badgeCountEnabled, value);
                _authService.CurrentUserProfile.NotificationSettings.DesktopBadgeCountEnabled = value;
                _ = _authService.UpdateNotificationSettingsAsync();
            }
        }

        public ReactiveCommand<Unit, Unit> EditProfileCommand { get; }
        public ReactiveCommand<Unit, Unit> LogoutCommand { get; }

        public Action? CloseWindow { get; set; }

        public SettingsWindowViewModel()
        {
            _authService = App.Services.GetRequiredService<AuthService>();

            // Инициализируем поля из CurrentUserProfile
            _selectedLanguage = _authService.CurrentUserProfile.PreferredLanguage ?? "en";
            _notificationsEnabled = _authService.CurrentUserProfile.NotificationSettings.DesktopNotificationsEnabled;
            _badgeCountEnabled = _authService.CurrentUserProfile.NotificationSettings.DesktopBadgeCountEnabled;

            EditProfileCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                // Открыть окно редактирования профиля (не показано здесь)
            });

            LogoutCommand = ReactiveCommand.CreateFromTask(async () =>
            {
                await _authService.LogoutAsync();
                CloseWindow?.Invoke();
                App.Current.MainWindow.Close();
                var login = new Views.LoginWindow { DataContext = new LoginWindowViewModel() };
                if (login.DataContext is LoginWindowViewModel vm) vm.CloseWindow = () => login.Close();
                login.Show();
            });
        }
    }
}
