using Avalonia.Controls;
using FlsurfDesktop.Core.Services;
using FlsurfDesktop.Services; // Наш сервис для управления окнами
using FlsurfDesktop.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;
using System;
using System.Reactive;

namespace FlsurfDesktop.ViewModels
{
    public class MainWindowViewModel : ReactiveObject
    {
        // --- Зависимости, полученные через DI ---
        private readonly AuthService _authService;
        private readonly IViewManager _viewManager;
        private readonly IServiceProvider _serviceProvider; // Используется как "фабрика" для ViewModel'ей

        // --- Свойства для привязки к View ---
        private ReactiveObject? _currentView;

        /// <summary>
        /// Текущая ViewModel, которая будет отображаться в основной части окна.
        /// </summary>
        public ReactiveObject? CurrentView
        {
            get => _currentView;
            private set => this.RaiseAndSetIfChanged(ref _currentView, value);
        }

        public string CurrentUserName { get; }
        public string UserRole { get; }
        public bool IsFreelancer { get; }

        // --- Команды ---
        public ReactiveCommand<Unit, Unit> ShowDashboardCommand { get; }
        public ReactiveCommand<Unit, Unit> ShowNotificationsCommand { get; }
        public ReactiveCommand<Unit, Unit> OpenSettingsCommand { get; }
        public ReactiveCommand<Unit, Unit> LogoutCommand { get; }

        // --- Конструктор ---
        public MainWindowViewModel(
            AuthService authService,
            IViewManager viewManager,
            IServiceProvider serviceProvider)
        {
            _authService = authService;
            _viewManager = viewManager;
            _serviceProvider = serviceProvider;

            // Инициализация свойств на основе залогиненного пользователя
            var profile = _authService.CurrentUserProfile;
            CurrentUserName = profile?.FullName ?? "Unknown User";
            UserRole = profile?.Type ?? "Unknown";
            IsFreelancer = UserRole == "Freelancer";

            // Настройка команд
            ShowDashboardCommand = ReactiveCommand.Create(ShowDashboard);
            ShowNotificationsCommand = ReactiveCommand.Create(ShowNotifications);
            OpenSettingsCommand = ReactiveCommand.Create(_viewManager.ShowSettingsWindow);
            LogoutCommand = ReactiveCommand.Create(Logout);

            // Показываем дашборд по умолчанию при входе
            ShowDashboard();
        }

        // --- Логика команд ---

        private void ShowDashboard()
        {
            // Вместо создания View, мы создаем нужную ViewModel через DI
            // и устанавливаем ее как текущую. View подберется автоматически через DataTemplate.
            if (IsFreelancer)
            {
                CurrentView = _serviceProvider.GetRequiredService<FreelancerDashboardViewModel>();
            }
            else
            {
                CurrentView = _serviceProvider.GetRequiredService<ClientDashboardViewModel>();
            }
        }

        private void ShowNotifications()
        {
            CurrentView = _serviceProvider.GetRequiredService<NotificationsViewModel>();
        }

        private void Logout()
        {
            // ViewModel больше не управляет окнами.
            // Она просто выполняет свою часть работы и делегирует UI-логику ViewManager'у.
            _authService.LogoutAsync();
            _viewManager.RestartApplication();
        }
    }
}