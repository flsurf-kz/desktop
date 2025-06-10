using ReactiveUI;
using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Threading.Tasks;
using FlsurfDesktop.Core.Services;
using FlsurfDesktop.Services;

namespace FlsurfDesktop.ViewModels
{
    public class LoginWindowViewModel : ReactiveObject
    {
        // --- Зависимости, полученные через DI ---
        private readonly AuthService _authService;
        private readonly IViewManager _viewManager;

        // --- Свойства для привязки к View ---
        private string _email = "";
        public string Email
        {
            get => _email;
            set => this.RaiseAndSetIfChanged(ref _email, value);
        }

        private string _password = "";
        public string Password
        {
            get => _password;
            set => this.RaiseAndSetIfChanged(ref _password, value);
        }

        private string _statusMessage = "";
        public string StatusMessage
        {
            get => _statusMessage;
            private set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            private set => this.RaiseAndSetIfChanged(ref _isBusy, value);
        }

        // --- Команды ---
        public ReactiveCommand<Unit, Unit> LoginWithPasswordCommand { get; }
        public ReactiveCommand<Unit, Unit> LoginWithGoogleCommand { get; }

        public LoginWindowViewModel(AuthService authService, IViewManager viewManager)
        {
            _authService = authService;
            _viewManager = viewManager;

            // Определяем, когда кнопка логина активна:
            // когда поля Email и Password не пустые и когда не идет процесс входа
            var canLogin = this.WhenAnyValue(
                x => x.Email,
                x => x.Password,
                x => x.IsBusy,
                (email, pass, busy) =>
                    !string.IsNullOrWhiteSpace(email) &&
                    !string.IsNullOrWhiteSpace(pass) &&
                    !busy);

            LoginWithPasswordCommand = ReactiveCommand.CreateFromTask(LoginWithPasswordAsync, canLogin);

            // Команда для входа через Google (пока заглушка)
            LoginWithGoogleCommand = ReactiveCommand.Create(() =>
            {
                StatusMessage = "Login with Google is not implemented yet.";
            });
        }

        private async Task LoginWithPasswordAsync()
        {
            IsBusy = true;
            StatusMessage = "Logging in...";

            var result = await _authService.LoginWithCredentialsAsync(Email, Password);

            if (!result.Success)
            {
                StatusMessage = result.ErrorMessage;
                IsBusy = false;
                return;
            }

            // Если API требует подтверждения секретной фразой
            if (result.NeedsSecretPhrase)
            {
                StatusMessage = "Waiting for secret phrase verification...";
                // ViewModel просит ViewManager показать диалог и ждет результат
                bool verified = _viewManager.ShowSecretPhraseDialog(result.UserId);

                if (!verified)
                {
                    StatusMessage = "Secret phrase verification failed or was cancelled.";
                    IsBusy = false;
                    return;
                }
            }

            StatusMessage = "Success!";

            // Просим ViewManager показать главное окно и закрыть окно логина
            _viewManager.ShowMainWindowAndCloseLogin();
        }
    }
}