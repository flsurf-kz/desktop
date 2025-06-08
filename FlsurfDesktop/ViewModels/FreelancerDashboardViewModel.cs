using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using FlsurfDesktop.Core.Models;
using FlsurfDesktop.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using FlsurfDesktop.RestClient;

namespace FlsurfDesktop.ViewModels
{
    public class FreelancerDashboardViewModel : ReactiveObject
    {
        private readonly ApiService _api;
        private readonly SessionService _sessionService;

        public ObservableCollection<ContractSummary> Contracts { get; } = new();

        private ContractSummary? _selectedContract;
        public ContractSummary? SelectedContract
        {
            get => _selectedContract;
            set
            {
                this.RaiseAndSetIfChanged(ref _selectedContract, value);
                UpdateSessionButtons();
            }
        }

        public bool CanStartSession => SelectedContract != null && !_sessionService.IsActive;
        public bool CanStopSession => _sessionService.IsActive;

        public string CurrentSessionInfo { get; private set; } = "";
        public string TodayEarningsInfo { get; private set; } = "";
        public string MonthEarningsInfo { get; private set; } = "";

        public ReactiveCommand<Unit, Unit> LoadContractsCommand { get; }
        public ReactiveCommand<Unit, Unit> ViewContractDetailsCommand { get; }
        public ReactiveCommand<Unit, Unit> StartSessionCommand { get; }
        public ReactiveCommand<Unit, Unit> StopSessionCommand { get; }

        public FreelancerDashboardViewModel()
        {
            _api = App.Services.GetRequiredService<ApiService>();
            _sessionService = App.Services.GetRequiredService<SessionService>();

            LoadContractsCommand = ReactiveCommand.CreateFromTask(LoadContractsAsync);
            ViewContractDetailsCommand = ReactiveCommand.Create(ViewDetails);
            StartSessionCommand = ReactiveCommand.CreateFromTask(StartSessionAsync);
            StopSessionCommand = ReactiveCommand.CreateFromTask(StopSessionAsync);

            // Загружаем контракты при старте
            _ = LoadContractsAsync();

            // Подписываемся на изменения сессии, чтобы обновлять отображение статуса
            _sessionService.SessionStarted += OnSessionStarted;
            _sessionService.SessionEnded += OnSessionEnded;
            _sessionService.SessionPeriodicTick += OnSessionPeriodicTick;
        }

        private void UpdateSessionButtons()
        {
            this.RaisePropertyChanged(nameof(CanStartSession));
            this.RaisePropertyChanged(nameof(CanStopSession));
        }

        private async Task LoadContractsAsync()
        {
            Contracts.Clear();
            var list = await _api.Client.GetContractsList(new GetContractsListQuery());
            foreach (var c in list)
            {
                // Преобразуем ContractEntity → ContractSummary (модель для списка)
                Contracts.Add(new ContractSummary
                {
                    Id = c.Id,
                    Title = c.Job.Title,
                    Status = c.Status.ToString(),
                    RemainingBudget = c.RemainingBudget ?? 0,
                    CostPerHour = c.CostPerHour.Amount
                });
            }
        }

        private void ViewDetails()
        {
            if (SelectedContract == null)
                return;

            var vm = new ContractDetailViewModel(SelectedContract.Id);
            var view = new Views.ContractDetailView { DataContext = vm };
            view.ShowDialog(App.Current.MainWindow);
        }

        private async Task StartSessionAsync()
        {
            if (SelectedContract == null) return;

            // Оповещаем SessionService, чтобы он начал сессию
            await _sessionService.StartSessionAsync(SelectedContract.Id);

            UpdateSessionButtons();
        }

        private async Task StopSessionAsync()
        {
            await _sessionService.StopSessionAsync();
            UpdateSessionButtons();
        }

        private void OnSessionStarted(Guid contractId, DateTimeOffset startTime)
        {
            CurrentSessionInfo = $"Session started at {startTime.LocalDateTime:T}";
            this.RaisePropertyChanged(nameof(CurrentSessionInfo));
        }

        private void OnSessionEnded(Guid contractId, DateTimeOffset endTime, decimal earned)
        {
            CurrentSessionInfo = $"Last session ended at {endTime.LocalDateTime:T}, earned {earned:C}";
            this.RaisePropertyChanged(nameof(CurrentSessionInfo));
            // Перезагрузить контракты, чтобы обновить RemainingBudget
            _ = LoadContractsAsync();
        }

        private void OnSessionPeriodicTick(TimeSpan elapsed, decimal earnedSoFar)
        {
            TodayEarningsInfo = $"Elapsed: {elapsed:h\\:mm}, Earned so far: {earnedSoFar:C}";
            this.RaisePropertyChanged(nameof(TodayEarningsInfo));
        }
    }
}
