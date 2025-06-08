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
    public class NotificationsViewModel : ReactiveObject
    {
        private readonly ApiService _api;

        public ObservableCollection<NotificationDto> Notifications { get; } = new();

        public ReactiveCommand<Unit, Unit> ReloadCommand { get; }
        public ReactiveCommand<string, Unit> OpenLinkCommand { get; }

        public NotificationsViewModel()
        {
            _api = App.Services.GetRequiredService<ApiService>();
            ReloadCommand = ReactiveCommand.CreateFromTask(LoadNotificationsAsync);
            OpenLinkCommand = ReactiveCommand.Create<string>(OpenLink);

            _ = LoadNotificationsAsync();
        }

        private async Task LoadNotificationsAsync()
        {
            Notifications.Clear();
            var list = await _api.Client.GetNotifications(App.Services.GetRequiredService<AuthService>().CurrentUserProfile.Id);
            foreach (var n in list)
            {
                Notifications.Add(new NotificationDto
                {
                    Title = n.Title,
                    Text = n.Text,
                    Url = n.Data // допустим, Data хранит URL
                });
            }
        }

        private void OpenLink(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return;
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
    }
}
