using ReactiveUI;
using System;
using System.Reactive;
using System.Threading.Tasks;
using FlsurfDesktop.Core.Services;
using System.Reactive.Linq; // Нужен для ObserveOn

namespace FlsurfDesktop.ViewModels
{
    public class SecretPhraseWindowViewModel : ReactiveObject
    {
        private readonly AuthService _authService;

        private Guid _userId;
        private string _phrase = "";
        private string _statusMessage = "";
        private bool _isBusy;

        public bool IsVerified { get; private set; } = false;

        public string Phrase
        {
            get => _phrase;
            set => this.RaiseAndSetIfChanged(ref _phrase, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            private set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        public bool IsBusy
        {
            get => _isBusy;
            private set => this.RaiseAndSetIfChanged(ref _isBusy, value);
        }

        public ReactiveCommand<Unit, bool> VerifyCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }

        // 1. Конструктор теперь принимает ТОЛЬКО зависимости от DI.
        // Он больше не знает о userId.
        public SecretPhraseWindowViewModel(AuthService authService)
        {
            _authService = authService;

            var canVerify = this.WhenAnyValue(
                x => x.Phrase,
                x => x.IsBusy,
                (phrase, busy) => !string.IsNullOrWhiteSpace(phrase) && !busy);

            VerifyCommand = ReactiveCommand.CreateFromTask(VerifyAsync, canVerify);
            CancelCommand = ReactiveCommand.Create(Cancel);
        }

        // 2. Публичный метод для передачи динамических данных ПОСЛЕ создания ViewModel.
        public void Initialize(Guid userId)
        {
            _userId = userId;
        }

        private async Task<bool> VerifyAsync()
        {
            IsBusy = true;
            StatusMessage = "Verifying...";

            var ok = await _authService.VerifySecretPhraseAsync(_userId, Phrase);

            if (!ok)
            {
                StatusMessage = "Wrong secret phrase.";
                IsBusy = false;
                return false;
            }

            StatusMessage = "Success!";
            IsVerified = true;
            IsBusy = false;
            return true;
        }

        private void Cancel()
        {
            IsVerified = false;
        }
    }
}