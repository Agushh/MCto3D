using Avalonia;

namespace MCto3D.Services
{
    public static class LanguageService
    {
        public static string GetString(string key, string fallback = "")
        {
            if (Application.Current != null && Application.Current.TryGetResource(key, Avalonia.Styling.ThemeVariant.Default, out var resource) && resource is string str)
            {
                return str;
            }
            return string.IsNullOrEmpty(fallback) ? key : fallback;
        }
    }
}

