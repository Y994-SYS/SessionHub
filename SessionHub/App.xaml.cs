using System.Windows;
using SessionHub.Models;

namespace SessionHub
{
	public partial class App : Application
	{
		private static bool _isDemoMode = true; // Varsayılan demo modu
		public static bool IsDemoMode => _isDemoMode;

		public static void ActivateLicense()
		{
			_isDemoMode = false;
		}

		protected override void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);
			// Lisans kontrolü yapma, direkt ana pencereyi aç (demo modunda başlasın)
			var mainWindow = new MainWindow();
			mainWindow.Show();
		}
	}
}