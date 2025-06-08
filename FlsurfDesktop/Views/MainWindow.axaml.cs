using ReactiveUI;
using System;
using System.Reactive;
using System.Threading.Tasks;
using FlsurfDesktop.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using FlsurfDesktop.Views;

namespace FlsurfDesktop.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        private readonly AuthService _authService;
        private object _currentView = new object();

        public object CurrentView
        {
            get => _currentView;
            private set => this.RaiseAndSetIfChanged(ref _currentView, value);
        }

        public bool IsFreelancer { get; private set; }
        public bool IsClient { get; private set; }

        public ReactiveCommand<Unit, Unit> ShowFreelancerDashboardCommand { get; }
        public ReactiveCommand<Unit, Unit> ShowClientDashboardCommand { get; }
        public ReactiveCommand<Unit, Unit> ShowNotificationsCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenSettingsCommand { get; }
        public ReactiveCommand<Unit, Unit> LogoutCommand { get; }
        public ReactiveCommand<Unit, Unit> ExitCommand { get; }
        public ReactiveCommand<Unit, Unit> ShowWindowCommand { get; }
        public ReactiveCommand<Unit, Unit> StartSessionCommand { get; }
        public ReactiveCommand<Unit, Unit> StopSessionCommand { get; }

        public MainWindowViewModel()
        {
            _authService = App.Services.GetRequiredService<AuthService>();

            ShowFreelancerDashboardCommand = ReactiveCommand.Create(ShowFreelancerDashboard);
            ShowClientDashboardCommand = ReactiveCommand.Create(ShowClientDashboard);
            ShowNotificationsCommand = ReactiveCommand.Create(ShowNotifications);
            OpenSettingsCommand = ReactiveCommand.Create(OpenSettings);
            LogoutCommand = ReactiveCommand.CreateFromTask(LogoutAsync);
            ExitCommand = ReactiveCommand.Create(() => Environment.Exit(0));
            ShowWindowCommand = ReactiveCommand.Create(() =>
            {
                if (App.Current.MainWindow.WindowState == Avalonia.Controls.WindowState.Minimized)
                    App.Current.MainWindow.WindowState = Avalonia.Controls.WindowState.Normal;
                App.Current.MainWindow.Activate();
            });

            StartSessionCommand = ReactiveCommand.CreateFromTask(() =>
                App.Services.GetRequiredService<SessionService>().StartSessionAsync());
            StopSessionCommand = ReactiveCommand.CreateFromTask(() =>
                App.Services.GetRequiredService<SessionService>().StopSessionAsync());

            // Определяем роль при старте MainWindow (AuthService уже хранит профиль)
            var profile = _authService.CurrentUserProfile;
            IsFreelancer = profile != null && profile.Type == "Freelancer";
            IsClient = profile != null && profile.Type == "Client";

            // По умолчанию сразу показываем нужный Dashboard
            if (IsFreelancer)
                CurrentView = new FreelancerDashboardView { DataContext = new FreelancerDashboardViewModel() };
            else if (IsClient)
                CurrentView = new ClientDashboardView { DataContext = new ClientDashboardViewModel() };
            else
                CurrentView = new object(); // или пустая заглушка

            // Запустим загрузку уведомлений сразу
            _ = Task.Run(() => ShowNotifications());
        }

        private void ShowFreelancerDashboard()
        {
            CurrentView = new FreelancerDashboardView { DataContext = new FreelancerDashboardViewModel() };
        }

        private void ShowClientDashboard()
        {
            CurrentView = new ClientDashboardView { DataContext = new ClientDashboardViewModel() };
        }

        private void ShowNotifications()
        {
            CurrentView = new NotificationsView { DataContext = new NotificationsViewModel() };
        }

        private void OpenSettings()
        {
            var win = new SettingsWindow { DataContext = new SettingsWindowViewModel() };
            win.ShowDialog(App.Current.MainWindow);
        }

        private async Task LogoutAsync()
        {
            await _authService.LogoutAsync();
            // После логаута закрываем всё и возвращаемся в LoginWindow
            App.Current.MainWindow.Close();
            var login = new Views.LoginWindow { DataContext = new LoginWindowViewModel() };
            if (login.DataContext is LoginWindowViewModel vm)
                vm.CloseWindow = () => login.Close();
            login.Show();
        }
    }
}
