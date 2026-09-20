using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace PortDetective.Converters
{
    /// <summary>Converts bool to Visibility (true → Visible, false → Collapsed).</summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = parameter is string s && s == "invert";
            bool boolVal = value is bool b && b;
            if (invert) boolVal = !boolVal;
            return boolVal ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is Visibility v && v == Visibility.Visible;
    }

    /// <summary>Converts a null value to Visibility (null → Collapsed, non-null → Visible).</summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isNull = value is null || (value is string s && string.IsNullOrWhiteSpace(s));
            bool invert = parameter is string p && p == "invert";
            bool visible = invert ? isNull : !isNull;
            return visible ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    /// <summary>Maps TCP connection state strings to status badge brushes.</summary>
    public class StateToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value?.ToString() switch
            {
                "Listening"   => new SolidColorBrush(Color.FromRgb(0x3B, 0x9E, 0x6B)),  // green
                "Established" => new SolidColorBrush(Color.FromRgb(0x26, 0x8B, 0xD2)),  // blue
                "Time Wait"   => new SolidColorBrush(Color.FromRgb(0xCB, 0x8B, 0x27)),  // amber
                "Close Wait"  => new SolidColorBrush(Color.FromRgb(0xCB, 0x8B, 0x27)),  // amber
                _             => new SolidColorBrush(Color.FromRgb(0x88, 0x88, 0x88)),  // grey
            };
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    /// <summary>Returns true when a string is not null or empty.</summary>
    public class StringToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            !string.IsNullOrWhiteSpace(value?.ToString());

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }

    /// <summary>Inverts a boolean value.</summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool b ? !b : value;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool b ? !b : value;
    }

    /// <summary>Maps IsInUse bool on a QuickPort button to a brush.</summary>
    public class BoolToBrushConverter : IValueConverter
    {
        public Brush TrueBrush  { get; set; } = new SolidColorBrush(Color.FromRgb(0x3B, 0x9E, 0x6B));
        public Brush FalseBrush { get; set; } = new SolidColorBrush(Color.FromRgb(0x3A, 0x3D, 0x4A));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
            value is bool b && b ? TrueBrush : FalseBrush;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
            throw new NotImplementedException();
    }
}
