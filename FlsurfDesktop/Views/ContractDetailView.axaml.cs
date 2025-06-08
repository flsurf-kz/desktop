using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using FlsurfDesktop.Core.Models;
using FlsurfDesktop.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FlsurfDesktop.ViewModels
{
    public class ContractDetailViewModel : ReactiveObject
    {
        private readonly ApiService _api;
        private Guid _contractId;

        // Свойства для отображения:
        public string Title { get; private set; } = "";
        public string Status { get; private set; } = "";
        public string BudgetInfo { get; private set; } = "";
        public string CostPerHourInfo { get; private set; } = "";
        public string RemainingBudgetInfo { get; private set; } = "";

        public ObservableCollection<WorkSessionDto> WorkSessions { get; } = new();

        private string _comment = "";
        public string Comment
        {
            get => _comment;
            set => this.RaiseAndSetIfChanged(ref _comment, value);
        }

        public ReactiveCommand<Guid, Unit> ViewSessionDetailCommand { get; }
        public ReactiveCommand<Unit, Unit> SubmitSessionCommand { get; }
        public ReactiveCommand<Unit, Unit> CloseCommand { get; }

        public bool CanSubmitSession => WorkSessions != null && WorkSessions.Count > 0 && WorkSessions[^1].Status == "Pending";

        public Action? CloseWindow { get; set; }

        public ContractDetailViewModel(Guid contractId)
        {
            _contractId = contractId;
            _api = App.Services.GetRequiredService<ApiService>();

            ViewSessionDetailCommand = ReactiveCommand.CreateFromTask<Guid>(ViewSessionDetailAsync);
            SubmitSessionCommand = ReactiveCommand.CreateFromTask(SubmitSessionAsync);
            CloseCommand = ReactiveCommand.Create(() => CloseWindow?.Invoke());

            // Загружаем детали контракта
            _ = LoadContractDetailAsync();
        }

        private async Task LoadContractDetailAsync()
        {
            var detail = await _api.Client.GetContract(_contractId);
            Title = detail.Job.Title;
            Status = detail.Status.ToString();
            BudgetInfo = $"Budget: {detail.Budget.Amount:C}";
            CostPerHourInfo = $"Rate: {detail.CostPerHour.Amount:C}/hr";
            RemainingBudgetInfo = $"Remaining: {detail.RemainingBudget?.Amount:C}";

            this.RaisePropertyChanged(nameof(Title));
            this.RaisePropertyChanged(nameof(Status));
            this.RaisePropertyChanged(nameof(BudgetInfo));
            this.RaisePropertyChanged(nameof(CostPerHourInfo));
            this.RaisePropertyChanged(nameof(RemainingBudgetInfo));

            // Загрузка WorkSessions
            WorkSessions.Clear();
            var sessions = await _api.Client.GetSessionList(new GetWorkSessionListQuery { ContractId = _contractId });
            foreach (var s in sessions)
            {
                WorkSessions.Add(new WorkSessionDto(s.Id, s.StartDate, s.EndDate, s.Status.ToString()));
            }
            this.RaisePropertyChanged(nameof(WorkSessions));
        }

        private async Task ViewSessionDetailAsync(Guid sessionId)
        {
            var vm = new SessionDetailViewModel(sessionId);
            var view = new Views.SessionDetailView { DataContext = vm };
            view.ShowDialog(App.Current.MainWindow);
        }

        private async Task SubmitSessionAsync()
        {
            // Посылаем комментарий и помечаем последнюю рабочую сессию как «Submitted»
            var pending = WorkSessions[^1];
            await _api.Client.SubmitSession(new SubmitWorkSessionCommand
            {
                SessionId = pending.Id,
                Comment = Comment
            });
            CloseWindow?.Invoke();
        }
    }
}
