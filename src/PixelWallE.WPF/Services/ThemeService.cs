using System;
using System.Linq;
using System.Windows;

namespace PixelWallE.WPF.Services;

public enum Theme { Light, Dark }

public static class ThemeService
{
    public static void ApplyTheme(Theme theme)
    {
        var app = (App)Application.Current;
        var dictionaries = app.Resources.MergedDictionaries;

        // Quitar el tema antiguo
        var existingTheme = dictionaries.FirstOrDefault(d => d.Source != null && d.Source.OriginalString.Contains("Theme."));
        if (existingTheme != null)
        {
            dictionaries.Remove(existingTheme);
        }

        // Añadir el nuevo tema
        var themeUri = new Uri($"/Themes/Theme.{theme}.xaml", UriKind.Relative);
        var newTheme = new ResourceDictionary { Source = themeUri };
        dictionaries.Add(newTheme);
    }
}