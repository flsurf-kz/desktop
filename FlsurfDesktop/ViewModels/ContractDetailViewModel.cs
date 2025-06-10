using FlsurfDesktop.Models; // Понадобится для WorkSessionViewModel
using FlsurfDesktop.RestClient;
using FlsurfDesktop.Services;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace FlsurfDesktop.ViewModels
{
    public class ContractDetailViewModel : ReactiveObject
    {
        // --- Зависимости ---
        private readonly IApiService _api;
        private readonly IViewManager _viewManager;

        // --- Свойства для привязки ---
        private ContractEntity? _contract;
        public ContractEntity? Contract
        {
            get => _contract;
            private set => this.RaiseAndSetIfChanged(ref _contract, value);
        }

        public ObservableCollection<WorkSessionViewModel> WorkSessions { get; } = new();

        private string _comment = "";
        public string Comment
        {
            get => _comment;
            set => this.RaiseAndSetIfChanged(ref _comment, value);
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

        public bool CanSubmitSession => WorkSessions.Any() && WorkSessions.Last().Status == "Pending";

        // --- Команды ---
        public ReactiveCommand<Guid, Unit> LoadDataCommand { get; }
        public ReactiveCommand<WorkSessionViewModel, Unit> ViewSessionDetailCommand { get; }
        public ReactiveCommand<Unit, Unit> SubmitSessionCommand { get; }
        public ReactiveCommand<Unit, Unit> CloseCommand { get; }

        // Делегат для закрытия окна. Устанавливается из code-behind.
        public Action? CloseWindow { get; set; }

        public ContractDetailViewModel(IApiService api, IViewManager viewManager)
        {
            _api = api;
            _viewManager = viewManager;

            // Команда для загрузки всех данных
            LoadDataCommand = ReactiveCommand.CreateFromTask<Guid>(LoadContractAndSessionsAsync);

            // Подписка на результат загрузки
            LoadDataCommand.Subscribe(contractId =>
            {
                this.RaisePropertyChanged(nameof(CanSubmitSession));
            });

            // Обработка ошибок
            LoadDataCommand.ThrownExceptions.Subscribe(ex => ErrorMessage = $"Failed to load data: {ex.Message}");
            LoadDataCommand.IsExecuting.BindTo(this, x => x.IsLoading);

            // Команда для просмотра деталей сессии
            ViewSessionDetailCommand = ReactiveCommand.Create<WorkSessionViewModel>(session =>
            {
                // Делегируем открытие окна ViewManager'у
                _viewManager.ShowSessionDetailWindow(session.Id);
            });

            // Команда для отправки сессии
            var canSubmit = this.WhenAnyValue(x => x.CanSubmitSession);
            SubmitSessionCommand = ReactiveCommand.CreateFromTask(SubmitSessionAsync, canSubmit);

            CloseCommand = ReactiveCommand.Create(() => CloseWindow?.Invoke());
        }

        // Асинхронный метод для инициализации
        public async Task InitializeAsync(Guid contractId)
        {
            await LoadDataCommand.Execute(contractId);
        }

        private async Task<Guid> LoadContractAndSessionsAsync(Guid contractId)
        {
            // Загружаем детали контракта
            Contract = await _api.GetContractAsync(contractId);

            // Загружаем рабочие сессии
            WorkSessions.Clear();
            var sessions = await _api.GetSessionListAsync(new GetWorkSessionListQuery { ContractId = contractId });
            foreach (var s in sessions)
            {
                WorkSessions.Add(new WorkSessionViewModel(s));
            }

            return contractId;
        }

        private async Task SubmitSessionAsync()
        {
            if (!CanSubmitSession) return;

            var pendingSession = WorkSessions.Last();

            // ВАШ API (Client.cs) не имеет метода SubmitSession с комментарием.
            // Используем тот, что есть.
            await _api.SubmitSessionAsync(new SubmitWorkSessionCommand
            {
                SessionId = pendingSession.Id,
                // Comment = this.Comment // Это поле нужно добавить в SubmitWorkSessionCommand на бэкенде
            });

            CloseWindow?.Invoke();
        }
    }

    /// <summary>
    /// Вспомогательная ViewModel для одного элемента списка рабочих сессий.
    /// </summary>
    public class WorkSessionViewModel : ReactiveObject
    {
        public Guid Id { get; }
        public string Status { get; }
        public string DateInfo { get; }
        public string Duration { get; }

        public WorkSessionViewModel(WorkSessionEntity session)
        {
            Id = session.Id;
            Status = session.Status.ToString();
            DateInfo = $"Started: {session.StartDate.ToLocalTime():g}";

            var duration = (session.EndDate ?? DateTimeOffset.Now) - session.StartDate;
            Duration = $"Duration: {duration:h\\'h'\\ mm\\'m'}";
        }
    }
}