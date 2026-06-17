using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MTerminal.Views;

public class GitStatusColorConverter : IValueConverter
{
    private static readonly ISolidColorBrush Modified  = new SolidColorBrush(Color.Parse("#C8A84D"));
    private static readonly ISolidColorBrush Added = new SolidColorBrush(Color.Parse("#6DBF8B"));
    private static readonly ISolidColorBrush Deleted = new SolidColorBrush(Color.Parse("#C06878"));
    private static readonly ISolidColorBrush Untracked = new SolidColorBrush(Color.Parse("#7BA4C9"));
    private static readonly ISolidColorBrush Fallback = new SolidColorBrush(Color.Parse("#8090b0"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "M" => Modified,
            "A" => Added,
            "D" => Deleted,
            "?" => Untracked,
            "R" => Modified,
            "C" => Added,
            "U" => Deleted,
            _ => Fallback
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
