using System;
using FlsurfDesktop.RestClient; // сюда входят Entity-классы, сгенерированные NSwag

namespace FlsurfDesktop.Core.Models
{
    public class UserProfile
    {
        public Guid Id { get; }
        public string Email { get; }
        public string Type { get; }  // "Freelancer" или "Client"
        public NotificationSettingsModel NotificationSettings { get; }
        public string? SecretPhraseRequired { get; }

        public UserProfile(UserEntity entity)
        {
            Id = entity.Id;
            Email = entity.Email;
            Type = entity.Type.ToString();
            NotificationSettings = new NotificationSettingsModel
            {
                DesktopNotificationsEnabled = entity.NotificationSettings.DesktopNotificationsEnabled,
                DesktopBadgeCountEnabled = entity.NotificationSettings.DesktopBadgeCountEnabled,
                // … остальные поля
            };
            SecretPhraseRequired = entity.Protected ? "yes" : null;
        }
    }

    public class NotificationSettingsModel
    {
        public bool DesktopNotificationsEnabled { get; set; }
        public bool DesktopBadgeCountEnabled { get; set; }
        // … остальные поля из NotificationSettings
    }
}
