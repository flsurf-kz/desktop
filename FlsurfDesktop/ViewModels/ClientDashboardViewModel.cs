using FlsurfDesktop.Core.Services;
using FlsurfDesktop.Models; // Для ContractSummaryViewModel
using FlsurfDesktop.RestClient;
using FlsurfDesktop.Services;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace FlsurfDesktop.ViewModels
{
    public class ClientDashboardViewModel : ReactiveObject
    {
        // --- Зависимости ---
        private readonly IApiService _api;
        private readonly AuthService _authService;
        private readonly IViewManager _viewManager;

        // --- Свойства для привязки к UI ---
        public ObservableCollection<ContractSummaryDto> Contracts { get; } = new();

        private ContractSummaryDto? _selectedContract;
        public ContractSummaryDto? SelectedContract
        {
            get => _selectedContract;
            set => this.RaiseAndSetIfChanged(ref _selectedContract, value);
        }

        private bool _isLoading;
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

        // --- Команды ---
        public ReactiveCommand<Unit, Unit> LoadContractsCommand { get; }
        public ReactiveCommand<Unit, Unit> ViewContractDetailsCommand { get; }
        public ReactiveCommand<Unit, Unit> PayContractCommand { get; }

        public ClientDashboardViewModel(IApiService api, AuthService authService, IViewManager viewManager)
        {
            _api = api;
            _authService = authService;
            _viewManager = viewManager;

            // Команда для загрузки контрактов
            LoadContractsCommand = ReactiveCommand.CreateFromTask(LoadContractsAsync);
            LoadContractsCommand.ThrownExceptions.Subscribe(ex => ErrorMessage = $"Failed to load contracts: {ex.Message}");
            LoadContractsCommand.IsExecuting.BindTo(this, x => x.IsLoading);

            // Условие для активации кнопок: выбран контракт и не идет загрузка
            var canInteractWithSelected = this.WhenAnyValue(
                x => x.SelectedContract,
                x => x.IsLoading,
                (selected, busy) => selected != null && !busy);

            // Команда для просмотра деталей
            ViewContractDetailsCommand = ReactiveCommand.Create(ViewContractDetails, canInteractWithSelected);

            // Команда для оплаты
            PayContractCommand = ReactiveCommand.CreateFromTask(PayContractAsync, canInteractWithSelected);
            PayContractCommand.ThrownExceptions.Subscribe(ex => ErrorMessage = $"Payment failed: {ex.Message}");

            // Запускаем загрузку при создании ViewModel
            LoadContractsCommand.Execute();
        }

        private async Task LoadContractsAsync()
        {
            Contracts.Clear();
            var profile = _authService.CurrentUserProfile;
            if (profile == null)
            {
                ErrorMessage = "Cannot load contracts: user profile not found.";
                return;
            }

            // В вашем API у GetContractsListQuery нет EmployerId, но есть UserId и IsClient
            var query = new GetContractsListQuery { UserId = profile.Id, IsClient = true };
            var contractsFromApi = await _api.GetContractsListAsync(query);

            foreach (var contract in contractsFromApi)
            {
                Contracts.Add(new ContractSummaryDto());
            }
        }

        private void ViewContractDetails()
        {
            if (SelectedContract == null) return;
            // Делегируем открытие окна менеджеру
            _viewManager.ShowContractDetailWindow(SelectedContract.Id);
        }

        private async Task PayContractAsync()
        {
            if (SelectedContract == null) return;

            // ВНИМАНИЕ: Ваш API для StartPaymentFlowAsync возвращает CommandResult, а не URL.
            // CommandResult.Id может содержать URL или ID платежа, это зависит от реализации вашего бэкенда.
            // Код ниже является заглушкой, предполагающей, что ID и есть URL.
            var result = await _api.StartPaymentFlowAsync(new StartPaymentFlowCommand { ContractId = SelectedContract.Id });

            if (result.IsSuccess && !string.IsNullOrWhiteSpace(result.Id))
            {
                // Открытие ссылки в браузере - это побочный эффект.
                // В идеале это тоже должно быть в отдельном сервисе (IBrowserService),
                // чтобы ViewModel оставалась тестируемой.
                Process.Start(new ProcessStartInfo(result.Id) { UseShellExecute = true });
            }
            else
            {
                ErrorMessage = result.Message ?? "Could not initiate payment.";
            }
        }
    }
}