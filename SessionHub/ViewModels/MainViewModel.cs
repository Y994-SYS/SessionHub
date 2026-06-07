using Microsoft.Win32;
using SessionHub.Data;
using SessionHub.Models;
using SessionHub.Services;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.IO.Compression;

namespace SessionHub.ViewModels
{
	public class MainViewModel : INotifyPropertyChanged
	{
		private readonly DatabaseContext _db;
		private readonly ChromeManager _chromeManager;
		private Profile _selectedProfile;
		private string _newProfileName;
		private string _proxyPassword;
		private int _lastSelectedId = 0;

		public ObservableCollection<Profile> Profiles { get; set; }

		public Profile SelectedProfile
		{
			get => _selectedProfile;
			set
			{
				_selectedProfile = value;
				OnPropertyChanged();

				if (_selectedProfile != null)
				{
					_lastSelectedId = _selectedProfile.Id;
					ProxyPassword = _selectedProfile.ProxyPassword;
				}
				else
				{
					ProxyPassword = string.Empty;
				}

				CommandManager.InvalidateRequerySuggested();
			}
		}

		public string NewProfileName
		{
			get => _newProfileName;
			set
			{
				_newProfileName = value;
				OnPropertyChanged();
			}
		}

		public string ProxyPassword
		{
			get => _proxyPassword;
			set
			{
				_proxyPassword = value;
				OnPropertyChanged();
			}
		}

		public bool IsDemoMode => App.IsDemoMode;

		public RelayCommand AddProfileCommand { get; }
		public RelayCommand DeleteProfileCommand { get; }
		public RelayCommand LaunchProfileCommand { get; }
		public RelayCommand SaveProfileCommand { get; }
		public RelayCommand TestProxyCommand { get; }
		public RelayCommand ExportProfilesCommand { get; }
		public RelayCommand ImportProfilesCommand { get; }
		public RelayCommand CloneProfileCommand { get; }
		public RelayCommand OpenProfileFolderCommand { get; }
		public RelayCommand ImportCookiesCommand { get; }
		public RelayCommand ExportFullProfileCommand { get; }
		public RelayCommand AddLicenseCommand { get; }

		public MainViewModel()
		{
			_db = new DatabaseContext();
			_chromeManager = new ChromeManager();
			Profiles = new ObservableCollection<Profile>();

			LoadProfiles();

			AddProfileCommand = new RelayCommand(AddProfile);
			DeleteProfileCommand = new RelayCommand(DeleteProfile, param => SelectedProfile != null);
			LaunchProfileCommand = new RelayCommand(LaunchProfile, param => SelectedProfile != null);
			SaveProfileCommand = new RelayCommand(SaveProfile, param => SelectedProfile != null);
			TestProxyCommand = new RelayCommand(TestProxy, param => CanTestProxy());
			ExportProfilesCommand = new RelayCommand(ExportProfiles);
			ImportProfilesCommand = new RelayCommand(ImportProfiles);
			CloneProfileCommand = new RelayCommand(CloneProfile, param => SelectedProfile != null);
			OpenProfileFolderCommand = new RelayCommand(OpenProfileFolder, param => SelectedProfile != null);
			ImportCookiesCommand = new RelayCommand(ImportCookies, param => SelectedProfile != null);
			ExportFullProfileCommand = new RelayCommand(ExportFullProfile, param => SelectedProfile != null);
			AddLicenseCommand = new RelayCommand(OpenLicenseWindow);
		}

		private void OpenLicenseWindow(object obj)
		{
			var licenseWindow = new LicenseWindow();
			licenseWindow.ShowDialog();

			if (!App.IsDemoMode)
			{
				OnPropertyChanged(nameof(IsDemoMode));
				MessageBox.Show("Lisans başarıyla aktif edildi! Artık sınırsız profil ekleyebilirsiniz.",
					"Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
			}
		}

		private void ExportProfiles(object obj)
		{
			try
			{
				var dialog = new SaveFileDialog
				{
					Filter = "JSON files (*.json)|*.json|CSV files (*.csv)|*.csv",
					DefaultExt = "json",
					FileName = "SessionHub_Backup_" + DateTime.Now.ToString("yyyyMMdd")
				};

				if (dialog.ShowDialog() == true)
				{
					var profiles = _db.GetAllProfiles();

					if (dialog.FileName.EndsWith(".json"))
					{
						var json = JsonSerializer.Serialize(profiles, new JsonSerializerOptions { WriteIndented = true });
						File.WriteAllText(dialog.FileName, json);
					}
					else
					{
						var csv = new StringBuilder();
						csv.AppendLine("Name,ProxyType,ProxyIp,ProxyPort,UserAgent,ScreenResolution,Timezone");

						foreach (var p in profiles)
						{
							csv.AppendLine($"{p.Name},{p.ProxyType},{p.ProxyIp},{p.ProxyPort},{p.UserAgent},{p.ScreenResolution},{p.Timezone}");
						}

						File.WriteAllText(dialog.FileName, csv.ToString());
					}

					MessageBox.Show($"{profiles.Count} profil dışa aktarıldı!", "Başarılı",
						MessageBoxButton.OK, MessageBoxImage.Information);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Export hatası: {ex.Message}", "Hata",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void ExportFullProfile(object obj)
		{
			if (SelectedProfile == null) return;

			try
			{
				string basePath = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
					"SessionHub",
					"ChromeProfiles");

				string profilePath = Path.Combine(basePath, $"Profile_{SelectedProfile.Id}");

				if (!Directory.Exists(profilePath))
				{
					MessageBox.Show("Profil klasörü bulunamadı.", "Hata");
					return;
				}

				var dialog = new SaveFileDialog
				{
					Filter = "ZIP File (*.zip)|*.zip",
					FileName = $"{SelectedProfile.Name}_profile_backup.zip"
				};

				if (dialog.ShowDialog() == true)
				{
					if (File.Exists(dialog.FileName))
						File.Delete(dialog.FileName);

					ZipFile.CreateFromDirectory(profilePath, dialog.FileName);

					MessageBox.Show("Profil başarıyla export edildi.", "Başarılı",
						MessageBoxButton.OK, MessageBoxImage.Information);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Export hatası: {ex.Message}", "Hata",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void ImportProfiles(object obj)
		{
			try
			{
				var dialog = new OpenFileDialog
				{
					Filter = "JSON files (*.json)|*.json|CSV files (*.csv)|*.csv",
					DefaultExt = "json"
				};

				if (dialog.ShowDialog() == true)
				{
					int importCount = 0;
					var tempProfiles = new List<Profile>();

					if (dialog.FileName.EndsWith(".json"))
					{
						var json = File.ReadAllText(dialog.FileName);
						var profiles = JsonSerializer.Deserialize<List<Profile>>(json);
						if (profiles != null)
							tempProfiles = profiles;
					}
					else
					{
						var lines = File.ReadAllLines(dialog.FileName).Skip(1);
						foreach (var line in lines)
						{
							var parts = line.Split(',');
							if (parts.Length >= 7)
							{
								var p = new Profile
								{
									Name = parts[0],
									ProxyType = parts[1],
									ProxyIp = parts[2],
									ProxyPort = parts[3],
									UserAgent = parts[4],
									ScreenResolution = parts[5],
									Timezone = parts[6],
									CreatedAt = DateTime.Now
								};
								tempProfiles.Add(p);
							}
						}
					}

					int currentCount = Profiles.Count;
					int availableSlots = IsDemoMode ? 3 - currentCount : int.MaxValue;

					if (tempProfiles.Count > availableSlots && IsDemoMode)
					{
						var result = MessageBox.Show(
							$"Demo modunda en fazla 3 profil olabilir. Şu anda {currentCount} profiliniz var.\n" +
							$"{tempProfiles.Count} profil içe aktarılmaya çalışılıyor, sadece {availableSlots} tane aktarılacak.\n" +
							$"Devam etmek istiyor musunuz?",
							"Demo Sınırı",
							MessageBoxButton.YesNo,
							MessageBoxImage.Warning);

						if (result != MessageBoxResult.Yes)
							return;

						tempProfiles = tempProfiles.Take(availableSlots).ToList();
					}

					foreach (var p in tempProfiles)
					{
						p.Id = 0;
						p.Name = GetUniqueName(p.Name);
						_db.AddProfile(p);
						importCount++;
					}

					LoadProfiles();
					MessageBox.Show($"{importCount} profil içe aktarıldı!", "Başarılı",
						MessageBoxButton.OK, MessageBoxImage.Information);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Import hatası: {ex.Message}", "Hata",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private string GetUniqueName(string baseName)
		{
			string name = baseName;
			int counter = 1;
			while (Profiles.Any(p => p.Name == name))
			{
				name = $"{baseName}_{counter}";
				counter++;
			}
			return name;
		}

		private bool CanTestProxy()
		{
			return SelectedProfile != null &&
				   SelectedProfile.ProxyType != "YOK" &&
				   !string.IsNullOrEmpty(SelectedProfile.ProxyIp) &&
				   !string.IsNullOrEmpty(SelectedProfile.ProxyPort);
		}

		private void LoadProfiles()
		{
			Profiles.Clear();
			var profiles = _db.GetAllProfiles();
			foreach (var profile in profiles)
				Profiles.Add(profile);

			if (_lastSelectedId > 0)
			{
				var previouslySelected = Profiles.FirstOrDefault(p => p.Id == _lastSelectedId);
				if (previouslySelected != null)
					SelectedProfile = previouslySelected;
			}
		}

		private void OpenProfileFolder(object obj)
		{
			if (SelectedProfile == null) return;

			try
			{
				string basePath = Path.Combine(
					Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
					"SessionHub",
					"ChromeProfiles");
				string profilePath = Path.Combine(basePath, $"Profile_{SelectedProfile.Id}");

				if (!Directory.Exists(profilePath))
					Directory.CreateDirectory(profilePath);

				System.Diagnostics.Process.Start("explorer.exe", profilePath);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Klasör açılırken hata: {ex.Message}", "Hata",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void ImportCookies(object obj)
		{
			if (SelectedProfile == null) return;

			try
			{
				var dialog = new OpenFileDialog
				{
					Filter = "JSON Cookie Files (*.json)|*.json",
					Title = "Cookie dosyasını seç"
				};

				if (dialog.ShowDialog() == true)
				{
					string extFolder = Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
						"SessionHub",
						"Extensions",
						$"cookie_loader_{SelectedProfile.Id}");

					Directory.CreateDirectory(extFolder);
					string targetFile = Path.Combine(extFolder, "cookies.json");
					File.Copy(dialog.FileName, targetFile, true);

					MessageBox.Show("Cookie dosyası başarıyla eklendi.\n\nTarayıcı açıldığında kullanılabilir.",
						"Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
				}
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Cookie import hatası: {ex.Message}", "Hata",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void CloneProfile(object obj)
		{
			if (SelectedProfile == null) return;

			try
			{
				var clone = new Profile
				{
					Name = GetUniqueName(SelectedProfile.Name + "_clone"),
					ProxyType = SelectedProfile.ProxyType,
					ProxyIp = SelectedProfile.ProxyIp,
					ProxyPort = SelectedProfile.ProxyPort,
					ProxyUsername = SelectedProfile.ProxyUsername,
					ProxyPassword = SelectedProfile.ProxyPassword,
					UserAgent = SelectedProfile.UserAgent,
					ScreenResolution = SelectedProfile.ScreenResolution,
					Timezone = SelectedProfile.Timezone,
					EnableWebGLSpoofing = SelectedProfile.EnableWebGLSpoofing,
					EnableCanvasSpoofing = SelectedProfile.EnableCanvasSpoofing,
					CreatedAt = DateTime.Now
				};

				_db.AddProfile(clone);
				LoadProfiles();

				var newProfile = Profiles.FirstOrDefault(p => p.Name == clone.Name);
				if (newProfile != null)
				{
					string basePath = Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
						"SessionHub", "ChromeProfiles");
					string sourceFolder = Path.Combine(basePath, $"Profile_{SelectedProfile.Id}");
					string destFolder = Path.Combine(basePath, $"Profile_{newProfile.Id}");

					if (Directory.Exists(sourceFolder))
						CopyDirectory(sourceFolder, destFolder);

					SelectedProfile = newProfile;
				}

				MessageBox.Show($"Profil klonlandı!\n\nYeni profil: {clone.Name}",
					"Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Klonlama hatası: {ex.Message}", "Hata",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private static void CopyDirectory(string sourceDir, string destDir)
		{
			Directory.CreateDirectory(destDir);
			foreach (var file in Directory.GetFiles(sourceDir))
			{
				string destFile = Path.Combine(destDir, Path.GetFileName(file));
				File.Copy(file, destFile, true);
			}
			foreach (var subDir in Directory.GetDirectories(sourceDir))
			{
				string destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
				CopyDirectory(subDir, destSubDir);
			}
		}

		private void AddProfile(object obj)
		{
			if (string.IsNullOrWhiteSpace(NewProfileName))
			{
				MessageBox.Show("Profil adı giriniz!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (Profiles.Any(p => p.Name.Equals(NewProfileName, StringComparison.OrdinalIgnoreCase)))
			{
				MessageBox.Show("Bu isimde bir profil zaten var!", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			if (IsDemoMode && Profiles.Count >= 3)
			{
				MessageBox.Show("Demo modunda maksimum 3 profil oluşturabilirsiniz.\nLisans alarak sınırsız profile kavuşun!",
					"Demo Sınırı", MessageBoxButton.OK, MessageBoxImage.Warning);
				return;
			}

			var profile = new Profile
			{
				Name = NewProfileName,
				ProxyType = "YOK",
				ScreenResolution = "1920,1080",
				Timezone = "Europe/Istanbul",
				EnableWebGLSpoofing = true,
				EnableCanvasSpoofing = true,
				CreatedAt = DateTime.Now
			};

			_db.AddProfile(profile);
			LoadProfiles();
			NewProfileName = string.Empty;
			SelectedProfile = Profiles.FirstOrDefault(p => p.Name == profile.Name);
		}

		private void DeleteProfile(object obj)
		{
			if (SelectedProfile == null) return;

			var result = MessageBox.Show($"'{SelectedProfile.Name}' profili silinsin mi?\n\nBu işlem geri alınamaz!",
				"Onay", MessageBoxButton.YesNo, MessageBoxImage.Question);

			if (result == MessageBoxResult.Yes)
			{
				try
				{
					string basePath = Path.Combine(
						Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
						"SessionHub", "ChromeProfiles");
					string profileFolder = Path.Combine(basePath, $"Profile_{SelectedProfile.Id}");
					if (Directory.Exists(profileFolder))
						Directory.Delete(profileFolder, true);
				}
				catch (Exception ex)
				{
					MessageBox.Show($"Profil klasörü silinirken hata oluştu:\n{ex.Message}\n\nVeritabanı kaydı yine de silinecek.",
						"Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
				}

				_db.DeleteProfile(SelectedProfile.Id);
				_lastSelectedId = 0;
				LoadProfiles();
				SelectedProfile = null;
			}
		}

		private void LaunchProfile(object obj)
		{
			if (SelectedProfile == null) return;

			try
			{
				SelectedProfile.ProxyPassword = ProxyPassword;
				_chromeManager.LaunchProfile(SelectedProfile);
				SelectedProfile.LastUsed = DateTime.Now;
				_db.UpdateProfile(SelectedProfile);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Tarayıcı açılırken hata: {ex.Message}", "Hata",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private void SaveProfile(object obj)
		{
			if (SelectedProfile == null) return;

			try
			{
				SelectedProfile.ProxyPassword = ProxyPassword;
				_db.UpdateProfile(SelectedProfile);
				LoadProfiles();
				MessageBox.Show("Profil başarıyla kaydedildi!", "Başarılı",
					MessageBoxButton.OK, MessageBoxImage.Information);
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Kaydetme hatası: {ex.Message}", "Hata",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private async void TestProxy(object obj)
		{
			if (SelectedProfile == null) return;

			try
			{
				Mouse.OverrideCursor = Cursors.Wait;
				string publicIp = "";

				if (SelectedProfile.ProxyType == "HTTP")
					publicIp = await TestHttpProxyAsync();
				else if (SelectedProfile.ProxyType == "SOCKS5")
					publicIp = await TestSocks5ProxyAsync();

				Mouse.OverrideCursor = null;

				if (!string.IsNullOrEmpty(publicIp))
				{
					MessageBox.Show($"✅ Proxy ÇALIŞIYOR\n\nIP: {publicIp}\n\nProxy: {SelectedProfile.ProxyIp}:{SelectedProfile.ProxyPort}",
						"Proxy Test", MessageBoxButton.OK, MessageBoxImage.Information);
				}
			}
			catch (Exception ex)
			{
				Mouse.OverrideCursor = null;
				MessageBox.Show($"❌ Proxy çalışmıyor\n\nHata: {ex.Message}",
					"Proxy Test", MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		private async Task<string> TestHttpProxyAsync()
		{
			var proxy = new WebProxy($"{SelectedProfile.ProxyIp}:{SelectedProfile.ProxyPort}");
			if (!string.IsNullOrEmpty(SelectedProfile.ProxyUsername))
				proxy.Credentials = new NetworkCredential(SelectedProfile.ProxyUsername, SelectedProfile.ProxyPassword);

			var handler = new HttpClientHandler { Proxy = proxy, UseProxy = true };
			using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(10) };
			var response = await client.GetStringAsync("https://ipinfo.io/ip");
			return response.Trim();
		}

		private async Task<string> TestSocks5ProxyAsync()
		{
			return await Task.Run(() =>
			{
				string proxyIp = SelectedProfile.ProxyIp;
				int proxyPort = int.Parse(SelectedProfile.ProxyPort);
				string targetHost = "ipinfo.io";
				int targetPort = 80;

				using (var tcp = new System.Net.Sockets.TcpClient())
				{
					var connectResult = tcp.BeginConnect(proxyIp, proxyPort, null, null);
					if (!connectResult.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(5)))
						throw new Exception("Proxy sunucusuna bağlanılamadı (timeout)");
					tcp.EndConnect(connectResult);
					var stream = tcp.GetStream();

					// SOCKS5 auth
					if (!string.IsNullOrEmpty(SelectedProfile.ProxyUsername))
					{
						byte[] authRequest = { 0x05, 0x01, 0x02 };
						stream.Write(authRequest, 0, authRequest.Length);
						byte[] authResp = new byte[2];
						stream.Read(authResp, 0, 2);
						if (authResp[0] != 0x05 || authResp[1] != 0x02)
							throw new Exception("SOCKS5 kimlik doğrulama desteklenmiyor");

						byte[] username = Encoding.UTF8.GetBytes(SelectedProfile.ProxyUsername);
						byte[] password = Encoding.UTF8.GetBytes(SelectedProfile.ProxyPassword ?? "");
						byte[] authPacket = new byte[3 + username.Length + password.Length];
						authPacket[0] = 0x01;
						authPacket[1] = (byte)username.Length;
						Array.Copy(username, 0, authPacket, 2, username.Length);
						authPacket[2 + username.Length] = (byte)password.Length;
						Array.Copy(password, 0, authPacket, 3 + username.Length, password.Length);
						stream.Write(authPacket, 0, authPacket.Length);
						byte[] authResult = new byte[2];
						stream.Read(authResult, 0, 2);
						if (authResult[1] != 0x00)
							throw new Exception("SOCKS5 kimlik doğrulama başarısız");
					}
					else
					{
						byte[] authRequest = { 0x05, 0x01, 0x00 };
						stream.Write(authRequest, 0, authRequest.Length);
						byte[] authResp = new byte[2];
						stream.Read(authResp, 0, 2);
						if (authResp[0] != 0x05 || authResp[1] != 0x00)
							throw new Exception("SOCKS5 anonim auth başarısız");
					}

					// Connect to target
					byte[] connectCmd = new byte[4 + 1 + 1 + 2 + targetHost.Length];
					connectCmd[0] = 0x05;
					connectCmd[1] = 0x01;
					connectCmd[2] = 0x00;
					connectCmd[3] = 0x03;
					connectCmd[4] = (byte)targetHost.Length;
					byte[] hostBytes = Encoding.ASCII.GetBytes(targetHost);
					Array.Copy(hostBytes, 0, connectCmd, 5, hostBytes.Length);
					byte[] portBytes = BitConverter.GetBytes((ushort)targetPort);
					if (BitConverter.IsLittleEndian) Array.Reverse(portBytes);
					Array.Copy(portBytes, 0, connectCmd, 5 + hostBytes.Length, 2);
					stream.Write(connectCmd, 0, connectCmd.Length);

					byte[] response = new byte[10];
					int bytesRead = stream.Read(response, 0, response.Length);
					if (bytesRead < 10 || response[1] != 0x00)
						throw new Exception("SOCKS5 hedefe bağlanamadı");

					// HTTP GET
					string httpRequest = $"GET /ip HTTP/1.1\r\nHost: {targetHost}\r\nConnection: close\r\n\r\n";
					byte[] httpBytes = Encoding.ASCII.GetBytes(httpRequest);
					stream.Write(httpBytes, 0, httpBytes.Length);

					using (var ms = new MemoryStream())
					{
						byte[] buffer = new byte[4096];
						int received;
						while ((received = stream.Read(buffer, 0, buffer.Length)) > 0)
							ms.Write(buffer, 0, received);
						string responseText = Encoding.ASCII.GetString(ms.ToArray());
						var lines = responseText.Split('\n');
						foreach (var line in lines)
						{
							if (line.Trim().StartsWith("IP:"))
								return line.Trim().Substring(3).Trim();
						}
						string lastLine = lines.LastOrDefault()?.Trim();
						if (!string.IsNullOrEmpty(lastLine) && lastLine.Length < 20 && lastLine.Contains('.'))
							return lastLine;
						throw new Exception("IP adresi alınamadı");
					}
				}
			});
		}

		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}