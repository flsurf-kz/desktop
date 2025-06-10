using FlsurfDesktop.Core.Services;
using FlsurfDesktop.RestClient;
using ReactiveUI;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace FlsurfDesktop.ViewModels;

public class FreelancerDashboardViewModel : ReactiveObject
{
    private readonly IApiService _api;
    private readonly SessionService _sessionService;

    public ObservableCollection<ContractSummaryDto> Contracts { get; } = new();

    private ContractSummaryDto? _selectedContract;
    public ContractSummaryDto? SelectedContract
    {
        get => _selectedContract;
        set => this.RaiseAndSetIfChanged(ref _selectedContract, value);
    }

    private string _sessionStatusText = "Session is not active.";
    public string SessionStatusText
    {
        get => _sessionStatusText;
        set => this.RaiseAndSetIfChanged(ref _sessionStatusText, value);
    }

    public bool CanStartSession => SelectedContract != null && !_sessionService.IsActive;
    public bool CanStopSession => _sessionService.IsActive;

    public ReactiveCommand<Unit, Unit> StartSessionCommand { get; }
    public ReactiveCommand<Unit, Unit> StopSessionCommand { get; }

    public FreelancerDashboardViewModel(IApiService api, SessionService sessionService)
    {
        _api = api;
        _sessionService = sessionService;

        var canStart = this.WhenAnyValue(x => x.SelectedContract, x => x._sessionService.IsActive,
            (contract, isActive) => contract != null && !isActive);

        var canStop = this.WhenAnyValue(x => x._sessionService.IsActive);

        StartSessionCommand = ReactiveCommand.CreateFromTask(StartSession, canStart);
        StopSessionCommand = ReactiveCommand.CreateFromTask(_sessionService.StopSessionAsync, canStop);

        _sessionService.SessionStateChanged += UpdateSessionStatus;

        _ = LoadContractsAsync();
    }

    private async Task LoadContractsAsync()
    {
        Contracts.Clear();
        // TODO: Фильтр по FreelancerId нужно будет добавить в ваш API
        var contracts = await _api.GetContractsListAsync(new GetContractsListQuery());
        foreach (var contract in contracts)
        {
            Contracts.Add(new ContractSummaryDto());
        }
    }

    private Task StartSession()
    {
        if (SelectedContract == null) return Task.CompletedTask;
        return _sessionService.StartSessionAsync(SelectedContract.ContractId);
    }

    private void UpdateSessionStatus()
    {
        if (_sessionService.IsActive)
        {
            SessionStatusText = $"Session running for {SelectedContract?.ContractLabel}. Time: {_sessionService.Elapsed:hh\\:mm\\:ss}. Earned: {_sessionService.EarnedSoFar:C}";
        }
        else
        {
            SessionStatusText = "Session is not active. Select a contract to start.";
        }
        this.RaisePropertyChanged(nameof(CanStartSession));
        this.RaisePropertyChanged(nameof(CanStopSession));
    }
}