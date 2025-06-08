using ReactiveUI;
using System;
using System.Reactive;
using System.Threading.Tasks;
using FlsurfDesktop.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace FlsurfDesktop.ViewModels
{
    public class LoginWindowViewModel : ReactiveObject
    {
        private readonly AuthService _authService;
        private string _email = "";
        private string _password = "";
        private string _statusMessage = "";

        public string Email
        {
            get => _email;
            set => this.RaiseAndSetIfChanged(ref _email, value);
        }

        public string Password
        {
            get => _password;
            set => this.RaiseAndSetIfChanged(ref _password, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        public ReactiveCommand<Unit, Unit> LoginWithPasswordCommand { get; }
        public ReactiveCommand<Unit, Unit> LoginWithGoogleCommand { get; }

        // Делегат, который позволяет закрыть окно из VM
        public Action? CloseWindow { get; set; }

        public LoginWindowViewModel()
        {
            _authService = App.Services.GetRequiredService<AuthService>();

            LoginWithPasswordCommand = ReactiveCommand.CreateFromTask(LoginWithPasswordAsync);
            LoginWithGoogleCommand = ReactiveCommand.CreateFromTask(LoginWithGoogleAsync);
        }

        private async Task LoginWithPasswordAsync()
        {
            StatusMessage = "";
            var result = await _authService.LoginWithCredentialsAsync(Email, Password);
            if (!result.Success)
            {
                StatusMessage = result.ErrorMessage;
                return;
            }

            // Проверяем, нужно ли запрашивать секретную фразу:
            if (result.NeedsSecretPhrase)
            {
                // Открываем SecretPhraseWindow
                var secretVm = new SecretPhraseWindowViewModel(result.UserId);
                var secretWin = new Views.SecretPhraseWindow { DataContext = secretVm };
                secretVm.CloseWindow = () => secretWin.Close();

                secretWin.ShowDialog(App.Current.MainWindow);
                // Только после закрытия окна проверяем:
                if (!secretVm.IsVerified)
                {
                    StatusMessage = "Secret phrase verification failed.";
                    return;
                }
            }

            // Всё успешно: закрываем LoginWindow и открываем MainWindow
            CloseWindow?.Invoke();
        }

        private async Task LoginWithGoogleAsync()
        {
            StatusMessage = "";
            var result = await _authService.LoginWithOidcAsync();
            if (!result.Success)
            {
                StatusMessage = result.ErrorMessage;
                return;
            }

            // OIDC-логин → сразу в MainWindow
            CloseWindow?.Invoke();
        }
    }
}
