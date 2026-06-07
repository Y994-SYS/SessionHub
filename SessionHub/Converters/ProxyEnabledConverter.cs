using System;
using System.Globalization;
using System.Windows.Data;

namespace SessionHub.Converters
{
	public class ProxyEnabledConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (value == null)
				return false;

			string proxyType = value.ToString();

			return proxyType != "YOK";
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return null;
		}
	}
}