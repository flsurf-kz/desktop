using ReactiveUI;
using System;
using System.Reactive;
using System.Threading.Tasks;
using FlsurfDesktop.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FlsurfDesktop.ViewModels
{
    public class SecretPhraseWindowViewModel : ReactiveObject
    {
        private readonly AuthService _authService;
        private string _phrase = "";
        private string _statusMessage = "";
        public Guid UserId { get; }
        public bool IsVerified { get; private set; } = false;
        public Action? CloseWindow { get; set; }

        public string Phrase
        {
            get => _phrase;
            set => this.RaiseAndSetIfChanged(ref _phrase, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        public ReactiveCommand<Unit, Unit> VerifyCommand { get; }

        public SecretPhraseWindowViewModel(Guid userId)
        {
            UserId = userId;
            _authService = App.Services.GetRequiredService<AuthService>();
            VerifyCommand = ReactiveCommand.CreateFromTask(VerifyAsync);
        }

        private async Task VerifyAsync()
        {
            StatusMessage = "";
            var ok = await _authService.VerifySecretPhraseAsync(UserId, Phrase);
            if (!ok)
            {
                StatusMessage = "Wrong secret phrase.";
                return;
            }

            IsVerified = true;
            CloseWindow?.Invoke();
        }
    }
}
