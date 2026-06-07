using System;
using System.ComponentModel.DataAnnotations;

namespace SessionHub.Models
{
	public class Profile
	{
		[Key]
		public int Id { get; set; }

		public string Name { get; set; } // Profil adı: "Instagram Hesap 1"

		public string ChromeProfilePath { get; set; } // C:\Users\x\AppData\Local\Google\Chrome\User Data\Profile X

		public string ProxyType { get; set; } // HTTP, SOCKS5, YOK

		public string ProxyIp { get; set; }

		public string ProxyPort { get; set; }

		public string ProxyUsername { get; set; }

		public string ProxyPassword { get; set; }

		public string UserAgent { get; set; } // Özel UserAgent

		public DateTime CreatedAt { get; set; } = DateTime.Now;

		public DateTime LastUsed { get; set; }
		public string Timezone { get; set; }
		public string Notes { get; set; } // Notlar
		public string ScreenResolution { get; set; } = "1920,1080";
		public bool EnableWebGLSpoofing { get; set; } = true;
		public bool EnableCanvasSpoofing { get; set; } = true;
	}
}