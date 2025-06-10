using System;
using System.Threading.Tasks;
using FlsurfDesktop.Core.Models;
using FlsurfDesktop.RestClient;
using Microsoft.Extensions.Logging;

namespace FlsurfDesktop.Core.Services;

public record LoginResult(bool Success, bool NeedsSecretPhrase = false, Guid UserId = default, string ErrorMessage = "");

public class AuthService
{
    private readonly IApiService _apiClient;
    private readonly ILogger<AuthService> _log;

    public UserProfile? CurrentUserProfile { get; private set; }
    public string AccessToken { get; private set; } = "";

    public AuthService(IApiService apiClient, ILogger<AuthService> log)
    {
        _apiClient = apiClient;
        _log = log;
    }

    public async Task<LoginResult> LoginWithCredentialsAsync(string email, string password)
    {
        try
        {
            var response = await _apiClient.LoginAsync(new LoginUserSchema { Email = email, Password = password, RememberMe = true });

            // В NSwag-клиенте CommandResult.Data нетипизирован. Вам нужно будет смотреть,
            // что возвращает ваш API, и десериализовать это.
            // Для примера, предположим, что токен приходит в `response.Id`.
            AccessToken = response.Id; // ЗАГЛУШКА. Используйте реальные данные из ответа API.

            var me = await _apiClient.GetMeAsync();
            CurrentUserProfile = new UserProfile(me);

            // API должен возвращать поле, указывающее на необходимость ввода секретной фразы.
            // if (me.SecretPhraseRequired)
            //     return new LoginResult(true, true, CurrentUserProfile.Id);

            return new LoginResult(true, false, CurrentUserProfile.Id);
        }
        catch (ApiException ex)
        {
            _log.LogError(ex, "Login failed with status {StatusCode}: {Response}", ex.StatusCode, ex.Response);
            return new LoginResult(false, ErrorMessage: $"API Error: {ex.StatusCode}");
        }
    }

    public async Task<bool> VerifySecretPhraseAsync(Guid userId, string phrase)
    {
        // В вашем API нет метода VerifySecretPhrase. Это заглушка.
        // Вам нужно будет добавить этот метод в API и перегенерировать NSwag-клиент.
        await Task.Delay(500);
        return phrase == "12345"; // Заглушка для теста
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _apiClient.LogoutAsync();
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "Logout API call failed, clearing local data anyway.");
        }

        AccessToken = "";
        CurrentUserProfile = null;
    }
}