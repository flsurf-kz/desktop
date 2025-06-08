using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Threading.Tasks;
using FlsurfDesktop.Core.Models;
using FlsurfDesktop.Core.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;

namespace FlsurfDesktop.ViewModels
{
    public class ClientDashboardViewModel : ReactiveObject
    {
        private readonly ApiService _api;

        public ObservableCollection<ContractSummary> Contracts { get; } = new();

        private ContractSummary? _selectedContract;
        public ContractSummary? SelectedContract
        {
            get => _selectedContract;
            set => this.RaiseAndSetIfChanged(ref _selectedContract, value);
        }

        public ReactiveCommand<Unit, Unit> LoadContractsCommand { get; }
        public ReactiveCommand<Unit, Unit> ViewContractDetailsCommand { get; }
        public ReactiveCommand<Unit, Unit> PayContractCommand { get; }

        public ClientDashboardViewModel()
        {
            _api = App.Services.GetRequiredService<ApiService>();

            LoadContractsCommand = ReactiveCommand.CreateFromTask(LoadContractsAsync);
            ViewContractDetailsCommand = ReactiveCommand.Create(ViewContractDetails);
            PayContractCommand = ReactiveCommand.CreateFromTask(PayContractAsync);

            _ = LoadContractsAsync();
        }

        private async Task LoadContractsAsync()
        {
            Contracts.Clear();
            // В API есть метод getClientOrderInfo(userId), но есть и getContractsList с фильтрацией
            var profile = App.Services.GetRequiredService<AuthService>().CurrentUserProfile;
            var list = await _api.Client.GetContractsList(new GetContractsListQuery { /* фильтр по EmployerId = profile.Id */ });

            foreach (var c in list)
            {
                if (c.EmployerId != profile.Id) continue; // берем только «клиентские» контракты

                Contracts.Add(new ContractSummary
                {
                    Id = c.Id,
                    JobTitle = c.Job.Title,
                    Status = c.Status.ToString(),
                    TotalSpent = c.Budget.Amount - (c.RemainingBudget?.Amount ?? 0m),
                    RemainingBudget = c.RemainingBudget?.Amount ?? 0m
                });
            }
        }

        private void ViewContractDetails()
        {
            if (SelectedContract == null) return;

            var vm = new ContractDetailViewModel(SelectedContract.Id);
            var view = new Views.ContractDetailView { DataContext = vm };
            view.ShowDialog(App.Current.MainWindow);
        }

        private async Task PayContractAsync()
        {
            if (SelectedContract == null) return;

            // Например, открываем браузер на страницу оплаты (External URL):
            var payUrl = await _api.Client.StartPaymentFlow(new StartPaymentFlowCommand { ContractId = SelectedContract.Id });
            // payUrl может вернуться в CommandResult.Data как URL
            // Открываем во внешнем браузере:
            Process.Start(new ProcessStartInfo
            {
                FileName = payUrl.ToString(),
                UseShellExecute = true
            });
        }
    }
}
