using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace MCto3D.Converters;

public class SelectedThumbnailConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int selectedIndex && parameter != null && int.TryParse(parameter.ToString(), out int targetIndex))
        {
            return selectedIndex == targetIndex ? SolidColorBrush.Parse("#10B981") : SolidColorBrush.Parse("#251F3D");
        }
        return SolidColorBrush.Parse("#251F3D");
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
