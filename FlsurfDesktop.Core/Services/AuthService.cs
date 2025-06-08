using System;
using System.Threading.Tasks;
using Duende.IdentityModel.OidcClient;
using Duende.IdentityModel.OidcClient.Browser;
using FlsurfDesktop.Core.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FlsurfDesktop.RestClient; 

namespace FlsurfDesktop.Core.Services
{
    public class AuthService
    {
        private readonly IApiService _apiClient;       // NSwag-клиент
        private readonly OidcClient _oidcClient;   // для Google/OIDC
        private readonly ILogger<AuthService> _log;

        public UserProfile? CurrentUserProfile { get; private set; }
        public string AccessToken { get; private set; } = "";
        private string RefreshToken { get; set; } = "";

        public AuthService(IConfiguration cfg, ILogger<AuthService> log)
        {
            _log = log;
            // Инициализируем NSwag-клиент. Предположим, он зарегистрирован в DI:
            _apiClient = new Client(cfg["Api:BaseUrl"]);

            // Настраиваем OIDC ‒ Authorization Code + PKCE
            var oidcOpts = new OidcClientOptions
            {
                Authority = cfg["Oidc:Authority"],
                ClientId = cfg["Oidc:ClientId"],
                RedirectUri = cfg["Oidc:RedirectUri"],     // e.g. http://127.0.0.1:7890/
                Scope = "openid profile email offline_access",
                Browser = new SystemBrowser(port: new Uri(cfg["Oidc:RedirectUri"]).Port),
                Flow = OidcClientOptions.AuthenticationFlow.AuthorizationCode,
                Policy = new Policy { RequireAccessTokenHash = false }
            };
            _oidcClient = new OidcClient(oidcOpts);
        }

        public async Task<LoginResult> LoginWithCredentialsAsync(string email, string password)
        {
            try
            {
                var res = await _apiClient.Login(new LoginUserSchema { Email = email, Password = password });
                if (!res.Success)
                    return new LoginResult(false, needsSecretPhrase: false, res.Message ?? "Login failed");

                // После успешного логина получаем opaque-токен на сервере (Set-Cookie + dropdown body.Mvc)
                AccessToken = res.Data; // допустим, res.Data = строковый access_token
                RefreshToken = res.Data; // если сервер сразу дал refresh

                // Делаем запрос профиля:
                var profileEntity = await _apiClient.GetMyProfileInfo();
                CurrentUserProfile = new UserProfile(profileEntity);

                // Если у пользователя есть SecretPhrase в профиле (Entity.SecretPhraseRequired == true)
                if (profileEntity.SecretPhraseRequired)
                    return new LoginResult(true, needsSecretPhrase: true, profileEntity.Id);

                return new LoginResult(true, needsSecretPhrase: false, profileEntity.Id);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "LoginWithCredentials failed");
                return new LoginResult(false, needsSecretPhrase: false, ex.Message);
            }
        }

        public async Task<bool> VerifySecretPhraseAsync(Guid userId, string phrase)
        {
            try
            {
                var cmdRes = await _apiClient.VerifySecretPhrase(new VerifySecretPhraseCommand { UserId = userId, Phrase = phrase });
                if (!cmdRes.Success)
                    return false;

                // Получаем финальный профиль (access_token может быть тот же)
                var profileEntity = await _apiClient.GetMyProfileInfo();
                CurrentUserProfile = new UserProfile(profileEntity);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<LoginResult> LoginWithOidcAsync()
        {
            try
            {
                var loginRes = await _oidcClient.LoginAsync();
                if (loginRes.IsError)
                    return new LoginResult(false, false, loginRes.Error);

                AccessToken = loginRes.AccessToken;
                RefreshToken = loginRes.RefreshToken ?? "";

                // Прокидываем токен в API-клиент:
                (_apiClient as Client)!.AccessToken = AccessToken; // допустим, ваш C# клиент хранит AccessToken свойством

                // Дальше, как только токен установлен, запрашиваем профиль:
                var profileEntity = await _apiClient.GetMyProfileInfo();
                CurrentUserProfile = new UserProfile(profileEntity);
                return new LoginResult(true, false, profileEntity.Id);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "OIDC login failed");
                return new LoginResult(false, false, ex.Message);
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                // Уведомляем сервер, что логаут:
                await _apiClient.Logout();
            }
            catch { /* ignore */ }
            AccessToken = "";
            RefreshToken = "";
            CurrentUserProfile = null;
        }

        public async Task<bool> UpdateNotificationSettingsAsync()
        {
            if (CurrentUserProfile == null) return false;

            try
            {
                var settings = CurrentUserProfile.NotificationSettings;
                var cmd = new UpdateUserCommand
                {
                    UserId = CurrentUserProfile.Id,
                    NotificationSettings = new NotificationSettingsDto
                    {
                        DesktopNotificationsEnabled = settings.DesktopNotificationsEnabled,
                        DesktopBadgeCountEnabled = settings.DesktopBadgeCountEnabled,
                        // … другие поля из модели
                    }
                };
                var res = await _apiClient.UpdateUser(CurrentUserProfile.Id.ToString(), cmd);
                return res.Success;
            }
            catch
            {
                return false;
            }
        }
    }

    public record LoginResult(bool Success, bool NeedsSecretPhrase, string ErrorMessageOrUserId);
}
