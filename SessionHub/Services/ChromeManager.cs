using SessionHub.Models;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;

namespace SessionHub.Services
{
	public class ChromeManager
	{
		private string _chromePath;
		private string _userDataBasePath;
		private string _extensionBasePath;

		public ChromeManager()
		{
			_chromePath = @"C:\Program Files\Google\Chrome\Application\chrome.exe";
			if (!File.Exists(_chromePath))
				_chromePath = @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe";

			_userDataBasePath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"SessionHub", "ChromeProfiles");

			_extensionBasePath = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				"SessionHub", "Extensions");

			Directory.CreateDirectory(_userDataBasePath);
			Directory.CreateDirectory(_extensionBasePath);
		}

		public void LaunchProfile(Profile profile)
		{
			if (profile == null) return;

			// Profil klasörü
			string profileFolder = Path.Combine(_userDataBasePath, $"Profile_{profile.Id}");
			Directory.CreateDirectory(profileFolder);

			var args = new StringBuilder();

			// =================== PROFİL ===================
			args.Append($"--user-data-dir=\"{profileFolder}\" ");
			args.Append("--profile-directory=Default ");
			// Fingerprint koruma argümanları (canvas/webgl)
			args.Append("--disable-reading-from-canvas ");
			args.Append("--disable-2d-canvas-clip-aa ");
			args.Append("--disable-accelerated-2d-canvas ");
			args.Append("--disable-webgl ");
			args.Append("--disable-webgl2 ");
			// =================== EXTENSIONS ===================
			var extensions = new List<string>();

			// Cookie extension
			string cookieExtension = CreateCookieExtension(profile);
			if (!string.IsNullOrEmpty(cookieExtension))
				extensions.Add(cookieExtension);

			// Proxy auth extension
			if (!string.IsNullOrEmpty(profile.ProxyUsername))
			{
				string proxyExtension = CreateProxyAuthExtension(profile);
				if (!string.IsNullOrEmpty(proxyExtension))
					extensions.Add(proxyExtension);
			}

			// Extensionları ekle
			if (extensions.Count > 0)
			{
				string extensionArg = string.Join(",", extensions);
				args.Append($"--load-extension=\"{extensionArg}\" ");
			}

			// =================== PROXY ===================
			if (!string.IsNullOrEmpty(profile.ProxyIp) && profile.ProxyType != "YOK")
			{
				args.Append($"--proxy-server=\"{profile.ProxyType.ToLower()}://{profile.ProxyIp}:{profile.ProxyPort}\" ");

				if (profile.ProxyType == "SOCKS5")
				{
					args.Append("--proxy-bypass-list=\"<-loopback>\" ");
				}
			}

			// =================== USER AGENT ===================
			if (!string.IsNullOrEmpty(profile.UserAgent))
			{
				args.Append($"--user-agent=\"{profile.UserAgent}\" ");
			}

			// =================== EKRAN ÇÖZÜNÜRLÜĞÜ ===================
			if (!string.IsNullOrEmpty(profile.ScreenResolution))
			{
				var res = profile.ScreenResolution.Split(',');
				if (res.Length == 2)
				{
					args.Append($"--window-size={res[0].Trim()},{res[1].Trim()} ");
				}
			}

			// =================== BASIC ANTI-DETECTION ===================
			args.Append("--disable-blink-features=AutomationControlled ");
			args.Append("--disable-dev-shm-usage ");
			args.Append("--force-webrtc-ip-handling-policy=default_public_interface_only ");

			// =================== CHROME BAŞLAT ===================
			try
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = _chromePath,
					Arguments = args.ToString() + " https://ipinfo.io",
					UseShellExecute = false
				});

				profile.LastUsed = DateTime.Now;
			}
			catch (Exception ex)
			{
				MessageBox.Show($"Chrome başlatılamadı:\n{ex.Message}", "Hata",
					MessageBoxButton.OK, MessageBoxImage.Error);
			}
		}

		// =================== PROXY AUTH EXTENSION ===================
		private string CreateProxyAuthExtension(Profile profile)
		{
			string extFolder = Path.Combine(_extensionBasePath, $"proxy_auth_{profile.Id}");
			Directory.CreateDirectory(extFolder);

			var manifest = @"{
				""manifest_version"": 2,
				""name"": ""Proxy Auth"",
				""version"": ""1.0"",
				""permissions"": [""proxy"", ""webRequest"", ""webRequestBlocking"", ""<all_urls>""],
				""background"": {
					""scripts"": [""background.js""],
					""persistent"": true
				}
			}";

			string backgroundJs = $@"
				var config = {{
					mode: ""fixed_servers"",
					rules: {{
						singleProxy: {{
							scheme: ""{profile.ProxyType.ToLower()}"",
							host: ""{profile.ProxyIp}"",
							port: {profile.ProxyPort}
						}},
						bypassList: [""localhost""]
					}}
				}};

				chrome.proxy.settings.set({{value: config, scope: ""regular""}}, function() {{}});

				chrome.webRequest.onAuthRequired.addListener(
					function(details, callbackFn) {{
						callbackFn({{
							authCredentials: {{
								username: ""{profile.ProxyUsername}"",
								password: ""{profile.ProxyPassword}""
							}}
						}});
					}},
					{{urls: [""<all_urls>""]}},
					[""blocking""]
				);
			";

			File.WriteAllText(Path.Combine(extFolder, "manifest.json"), manifest);
			File.WriteAllText(Path.Combine(extFolder, "background.js"), backgroundJs);

			return extFolder;
		}
		private string CreateCookieExtension(Profile profile)
		{
			string extFolder = Path.Combine(_extensionBasePath, $"cookie_loader_{profile.Id}");
			Directory.CreateDirectory(extFolder);

			string manifest = @"{
        ""manifest_version"": 2,
        ""name"": ""Cookie Loader"",
        ""version"": ""1.0"",
        ""permissions"": [""cookies"", ""tabs"", ""<all_urls>""],
        ""background"": {
            ""scripts"": [""background.js""]
        }
    }";

			string background = @"
        fetch(chrome.runtime.getURL('cookies.json'))
        .then(response => response.json())
        .then(cookies => {
            cookies.forEach(cookie => {
                chrome.cookies.set(cookie);
            });
        });
    ";

			File.WriteAllText(Path.Combine(extFolder, "manifest.json"), manifest);
			File.WriteAllText(Path.Combine(extFolder, "background.js"), background);

			return extFolder;
		}
		private void CreateDefaultProfileFiles(string profilePath)
		{
			string defaultPath = Path.Combine(profilePath, "Default");
			Directory.CreateDirectory(defaultPath);
		}
	}
}