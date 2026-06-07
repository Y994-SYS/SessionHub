using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SessionHub.Converters
{
	public class NullToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			bool isReversed = parameter != null && bool.Parse(parameter.ToString());
			bool isNull = value == null;

			if (isReversed)
				return isNull ? Visibility.Visible : Visibility.Collapsed;

			return isNull ? Visibility.Collapsed : Visibility.Visible;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}