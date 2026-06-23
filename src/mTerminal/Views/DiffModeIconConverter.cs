using System.Globalization;
using Avalonia.Data.Converters;
using Material.Icons;

namespace mTerminal.Views;

public class DiffModeIconConverter : IValueConverter
{
    public static readonly DiffModeIconConverter Instance = new();
    public static readonly FuncValueConverter<bool, double> BoolToOpacity = new(v => v ? 1.0 : 0.35);

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is true ? MaterialIconKind.ViewSequential : MaterialIconKind.ViewSplitVertical;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
