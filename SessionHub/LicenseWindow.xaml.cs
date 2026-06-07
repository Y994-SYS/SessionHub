using System.Windows;
using SessionHub.Models;

namespace SessionHub
{
	public partial class LicenseWindow : Window
	{
		public bool IsActivated { get; private set; } = false;

		public LicenseWindow()
		{
			InitializeComponent();
			txtHwid.Text = LicenseManager.GetHardwareId();
		}

		private void BtnCopy_Click(object sender, RoutedEventArgs e)
		{
			Clipboard.SetText(txtHwid.Text);
			MessageBox.Show("HWID kopyalandı! Satıcıya gönderin.", "Bilgi");
		}

		private void TxtKey_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
		{
			btnActivate.IsEnabled = txtKey.Text.Length >= 19;
		}

		private void BtnActivate_Click(object sender, RoutedEventArgs e)
		{
			string hwid = LicenseManager.GetHardwareId();
			string key = txtKey.Text.Trim().ToUpper();

			if (LicenseManager.ValidateKey(key, hwid))
			{
				LicenseManager.SaveLicense(key);
				App.ActivateLicense();   // Demo modunu kapat
				IsActivated = true;
				MessageBox.Show("✅ Lisans başarıyla aktive edildi!", "Başarılı",
					MessageBoxButton.OK, MessageBoxImage.Information);
				this.Close();
			}
			else
			{
				MessageBox.Show("❌ Geçersiz lisans anahtarı!\n\nKey'inizi kontrol edin veya satıcıyla iletişime geçin.",
					"Hata", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void BtnDemo_Click(object sender, RoutedEventArgs e)
		{
			var result = MessageBox.Show("Demo modunda sadece 3 profil oluşturabilirsiniz.\n\nDevam etmek istiyor musunuz?",
				"Demo Modu", MessageBoxButton.YesNo, MessageBoxImage.Question);

			if (result == MessageBoxResult.Yes)
			{
				this.Close();
			}
		}
	}
}