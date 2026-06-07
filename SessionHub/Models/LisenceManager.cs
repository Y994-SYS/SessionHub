using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace SessionHub.Models
{
	public static class LicenseManager
	{
		// BİLGİSAYARIN DNA'SI (Değişmez)
		public static string GetHardwareId()
		{
			string cpuId = GetCpuId();
			string diskId = GetDiskId();
			string combined = cpuId + diskId;

			using (var md5 = MD5.Create())
			{
				byte[] hash = md5.ComputeHash(Encoding.UTF8.GetBytes(combined));
				string hwid = BitConverter.ToString(hash).Replace("-", "");
				return hwid.Substring(0, 8); // Sadece ilk 8 karakter (yeterli)
			}
		}

		// KEY ÜRETME (Sizin uygulamanızda kullanılacak)
		public static string GenerateKey(string hwid)
		{
			// Gerçek üretimde sunucu tarafında yapılır, bu sadece gösterimlik
			using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("SessionHub_Secret_Salt_2025")))
			{
				byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(hwid + DateTime.Now.ToString("yyyyMM")));
				string hashStr = BitConverter.ToString(hash).Replace("-", "").Substring(0, 16);
				return $"{hwid.Substring(0, 4)}-{hwid.Substring(4, 4)}-{hashStr.Substring(0, 4)}-{hashStr.Substring(4, 4)}";
			}
		}

		public static bool ValidateKey(string key, string hwid)
		{
			if (string.IsNullOrEmpty(key) || key.Length != 19) return false;
			string[] parts = key.Split('-');
			if (parts.Length != 4) return false;

			string keyPrefix = parts[0] + parts[1];
			if (keyPrefix != hwid.Substring(0, 8).ToUpper()) return false;

			// Doğrulama kısmı: HMAC ile HWID + ay kontrolü
			using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes("SessionHub_Secret_Salt_2025")))
			{
				byte[] hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(hwid + DateTime.Now.ToString("yyyyMM")));
				string expectedSuffix = BitConverter.ToString(hash).Replace("-", "").Substring(0, 8).ToUpper();
				string givenSuffix = parts[2] + parts[3];
				return expectedSuffix == givenSuffix;
			}
		}

		// LİSANS KAYDETME
		public static void SaveLicense(string key)
		{
			string folder = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"SessionHub");

			if (!Directory.Exists(folder))
				Directory.CreateDirectory(folder);

			string file = Path.Combine(folder, "license.key");
			File.WriteAllText(file, key);
		}

		// LİSANS YÜKLEME
		public static string LoadLicense()
		{
			string file = Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"SessionHub", "license.key");

			return File.Exists(file) ? File.ReadAllText(file) : null;
		}

		// YARDIMCI METODLAR
		private static string GetCpuId()
		{
			try
			{
				var mc = new System.Management.ManagementClass("win32_processor");
				foreach (var mo in mc.GetInstances())
				{
					return mo.Properties["processorID"].Value.ToString();
				}
			}
			catch { }
			return "CPU12345";
		}

		private static string GetDiskId()
		{
			try
			{
				var mc = new System.Management.ManagementClass("Win32_DiskDrive");
				foreach (var mo in mc.GetInstances())
				{
					return mo.Properties["Model"].Value.ToString();
				}
			}
			catch { }
			return "DISK12345";
		}
	}
}