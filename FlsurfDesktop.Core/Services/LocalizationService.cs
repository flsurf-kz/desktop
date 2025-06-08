using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace FlsurfDesktop.Core.Services
{
    public static class LocalizationService
    {
        public static void SetCulture(string lang)
        {
            var culture = new System.Globalization.CultureInfo(lang);
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = culture;
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = culture;

            // Перезагрузка ResourceDictionary:
            var app = (App)Application.Current;
            app.Resources.MergedDictionaries.Clear();
            app.Resources.MergedDictionaries.Add(new ResourceInclude
            {
                Source = new Uri($"avares://FlsurfDesktop/Resources/Strings/Strings.{lang}.axaml")
            });
        }
    }

}
