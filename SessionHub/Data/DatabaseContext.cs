using System.Data.SQLite;
using Dapper; // NuGet: Dapper (SQLite kolaylığı için)
using SessionHub.Models;
using System.Collections.Generic;
using System.Linq;

namespace SessionHub.Data
{
	public class DatabaseContext
	{
		private string _connectionString;

		public DatabaseContext()
		{
			string dbPath = System.IO.Path.Combine(
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"SessionHub",
				"profiles.db");

			System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(dbPath));
			_connectionString = $"Data Source={dbPath}";

			InitializeDatabase();
		}

		private void InitializeDatabase()
		{
			using var connection = new SQLiteConnection(_connectionString);

			// Tabloyu yeni kolonlarla oluştur (Eğer yoksa)
			connection.Execute(@"
        CREATE TABLE IF NOT EXISTS Profiles (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Name TEXT NOT NULL,
            ChromeProfilePath TEXT,
            ProxyType TEXT,
            ProxyIp TEXT,
            ProxyPort TEXT,
            ProxyUsername TEXT,
            ProxyPassword TEXT,
            UserAgent TEXT,
            ScreenResolution TEXT DEFAULT '1920,1080',
            Timezone TEXT DEFAULT 'Europe/Istanbul',
            EnableWebGLSpoofing INTEGER DEFAULT 1,
            EnableCanvasSpoofing INTEGER DEFAULT 1,
            CreatedAt TEXT,
            LastUsed TEXT,
            Notes TEXT
        )
    ");

			// Eksik kolonları ekle (Eski tablo varsa)
			try { connection.Execute("ALTER TABLE Profiles ADD COLUMN ScreenResolution TEXT DEFAULT '1920,1080'"); } catch { }
			try { connection.Execute("ALTER TABLE Profiles ADD COLUMN Timezone TEXT DEFAULT 'Europe/Istanbul'"); } catch { }
			try { connection.Execute("ALTER TABLE Profiles ADD COLUMN EnableWebGLSpoofing INTEGER DEFAULT 1"); } catch { }
			try { connection.Execute("ALTER TABLE Profiles ADD COLUMN EnableCanvasSpoofing INTEGER DEFAULT 1"); } catch { }
		}

		public List<Profile> GetAllProfiles()
		{
			using var connection = new SQLiteConnection(_connectionString);
			return connection.Query<Profile>("SELECT * FROM Profiles ORDER BY CreatedAt DESC").ToList();
		}

		public void AddProfile(Profile profile)
		{
			using var connection = new SQLiteConnection(_connectionString);
			var sql = @"INSERT INTO Profiles 
                (Name, ChromeProfilePath, ProxyType, ProxyIp, ProxyPort, ProxyUsername, ProxyPassword, UserAgent, CreatedAt, LastUsed, Notes)
                VALUES 
                (@Name, @ChromeProfilePath, @ProxyType, @ProxyIp, @ProxyPort, @ProxyUsername, @ProxyPassword, @UserAgent, @CreatedAt, @LastUsed, @Notes)";
			connection.Execute(sql, profile);
		}

		public void DeleteProfile(int id)
		{
			using var connection = new SQLiteConnection(_connectionString);
			connection.Execute("DELETE FROM Profiles WHERE Id = @Id", new { Id = id });
		}
		public void UpdateProfile(Profile profile)
		{
			using var connection = new SQLiteConnection(_connectionString);
			var sql = @"UPDATE Profiles SET 
        Name = @Name,
        ProxyType = @ProxyType,
        ProxyIp = @ProxyIp,
        ProxyPort = @ProxyPort,
        ProxyUsername = @ProxyUsername,
        ProxyPassword = @ProxyPassword,
        UserAgent = @UserAgent,
        ScreenResolution = @ScreenResolution,
        Timezone = @Timezone,
        EnableWebGLSpoofing = @EnableWebGLSpoofing,
        EnableCanvasSpoofing = @EnableCanvasSpoofing,
        Notes = @Notes,
        LastUsed = @LastUsed
        WHERE Id = @Id";
			connection.Execute(sql, profile);
		}
	}
}