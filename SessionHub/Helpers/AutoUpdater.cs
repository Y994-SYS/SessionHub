using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace SessionHub.Helpers
{
	public static class AutoUpdater
	{
		// Kendi güncel repo adresin tanımlandı
		private static readonly string GitHubApiUrl = "https://api.github.com/repos/Y994-SYS/SessionHub/releases/latest";
		private static readonly string CurrentVersion = "0.9.0"; // Mevcut sürüm

		public static async Task CheckForUpdates()
		{
			try
			{
				MessageBox.Show("1. ADIM: Güncelleme kontrol fonksiyonu tetiklendi!", "Dedektif");

				using var client = new HttpClient();
				client.DefaultRequestHeaders.Add("User-Agent", "SessionHub-Updater");

				MessageBox.Show($"2. ADIM: GitHub'a istek atılıyor...\nURL: {GitHubApiUrl}", "Dedektif");
				var response = await client.GetAsync(GitHubApiUrl);

				MessageBox.Show($"3. ADIM: GitHub'dan yanıt geldi!\nDurum Kodu: {response.StatusCode}", "Dedektif");
				if (!response.IsSuccessStatusCode)
				{
					MessageBox.Show($"BAŞARISIZ: GitHub API başarılı dönmedi. Kod: {response.StatusCode}", "Dedektif");
					return;
				}

				var json = await response.Content.ReadAsStringAsync();
				using var doc = JsonDocument.Parse(json);

				var latestVersion = doc.RootElement.GetProperty("tag_name").GetString();
				var assets = doc.RootElement.GetProperty("assets");

				MessageBox.Show($"4. ADIM: JSON okundu.\nGitHub'daki Versiyon: {latestVersion}\nBulunan Asset Sayısı: {assets.GetArrayLength()}", "Dedektif");

				if (assets.GetArrayLength() == 0)
				{
					MessageBox.Show("BAŞARISIZ: GitHub release içinde hiç dosya (asset) bulunamadı!", "Dedektif");
					return;
				}

				var downloadUrl = assets[0].GetProperty("browser_download_url").GetString();

				// Sürüm karşılaştırması öncesi değerleri görelim
				MessageBox.Show($"5. ADIM: Karşılaştırma yapılıyor...\nKoddaki Sürüm: {CurrentVersion}\nGitHub'daki Sürüm: {latestVersion}", "Dedektif");

				if (IsNewerVersion(latestVersion))
				{
					MessageBox.Show("6. ADIM: Yeni sürüm algılandı! Onay kutusu açılıyor...", "Dedektif");

					var result = MessageBox.Show($"Yeni sürüm ({latestVersion}) mevcut! Güncellemek istiyor musunuz?",
						"Güncelleme", MessageBoxButton.YesNo, MessageBoxImage.Question);

					if (result == MessageBoxResult.Yes)
					{
						MessageBox.Show("7. ADIM: İndirme başlıyor...", "Dedektif");
						await DownloadAndInstallUpdate(downloadUrl);
					}
				}
				else
				{
					MessageBox.Show("BİTTİ: GitHub'daki sürüm yerel sürümden büyük değil. Güncelleme yok.", "Dedektif");
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"KRİTİK HATA YAKALANDI:\n{ex.Message}\n\nDetay: {ex.InnerException?.Message}",
								"Hata Tanılama", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private static bool IsNewerVersion(string latest)
		{
			if (string.IsNullOrEmpty(latest)) return false;

			try
			{
				// Başındaki 'v' harfini temizle
				latest = latest.TrimStart('v').Trim();

				// Güvenli sürüm karşılaştırması (1.0.10 > 1.0.2 doğruluğu sağlar)
				var current = new Version(CurrentVersion);
				var latestVer = new Version(latest);

				return latestVer > current;
			}
			catch
			{
				return false; // Sürüm formatı hatalıysa işlemi pas geç
			}
		}

		// async void yerine async Task yapıldı
		private static async Task DownloadAndInstallUpdate(string downloadUrl)
		{
			try
			{
				using var client = new HttpClient();

				// Temp path alırken uzantıyı temiz ekleyelim
				var tempFile = Path.Combine(Path.GetTempPath(), $"SessionHub_Update_{Guid.NewGuid().ToString().Substring(0, 8)}.exe");

				var response = await client.GetAsync(downloadUrl);
				using (var fs = new FileStream(tempFile, FileMode.Create, FileAccess.Write, FileShare.None))
				{
					await response.Content.CopyToAsync(fs);
				}

				// İndirilen dosyanın varlığını ve boyutunu ufak bir teyit edelim
				if (!File.Exists(tempFile) || new FileInfo(tempFile).Length == 0)
				{
					throw new FileNotFoundException("Güncelleme dosyası indirilemedi.");
				}

				var startInfo = new System.Diagnostics.ProcessStartInfo
				{
					FileName = tempFile,
					Arguments = "/update",
					UseShellExecute = true
				};

				System.Diagnostics.Process.Start(startInfo);
				Application.Current.Shutdown(); // Eski uygulamayı kapat
			}
			catch (Exception ex)
			{
				// Hatayı gizlemek yerine ekranda gösteriyoruz
				MessageBox.Show($"Güncelleme kontrolü sırasında hata oluştu:\n{ex.Message}\n\nDetay: {ex.InnerException?.Message}",
								"Hata Tanılama", MessageBoxButton.OK, MessageBoxImage.Warning);
			}
		}
	}
}