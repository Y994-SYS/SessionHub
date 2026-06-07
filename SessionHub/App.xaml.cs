using System;
using System.Windows;
using System.Windows.Threading;
using SessionHub.Models;

namespace SessionHub
{
	public partial class App : Application
	{
		private static bool _isDemoMode = true;
		public static bool IsDemoMode => _isDemoMode;

		public static void ActivateLicense()
		{
			_isDemoMode = false;
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			// 1. Ana pencereyi oluştur ve hemen göster
			var mainWindow = new MainWindow();
			mainWindow.Show();

			// 2. Dispatcher kullanarak, arayüz çizildikten hemen sonra arka planda fonksiyonu tetikle
			Dispatcher.CurrentDispatcher.BeginInvoke(new Action(async () =>
			{
				try
				{
					await Helpers.AutoUpdater.CheckForUpdates();
				}
				catch (Exception ex)
				{
					System.Diagnostics.Debug.WriteLine($"Güncelleme tetikleme hatası: {ex.Message}");
				}
			}), DispatcherPriority.Background);
		}
	}
}