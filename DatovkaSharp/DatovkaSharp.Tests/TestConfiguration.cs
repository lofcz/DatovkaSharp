using System;
using System.IO;
using System.Text.Json;

namespace DatovkaSharp.Tests
{
    public class TestAccount
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class AppConfig
    {
        public TestAccount Account1 { get; set; } = new TestAccount();
        public TestAccount Account2 { get; set; } = new TestAccount();
    }

    public static class TestConfiguration
    {
        private static AppConfig? _config;
        private static readonly object _lock = new object();

        public static AppConfig Config
        {
            get
            {
                if (_config == null)
                {
                    lock (_lock)
                    {
                        if (_config == null)
                        {
                            LoadConfiguration();
                        }
                    }
                }
                return _config!;
            }
        }

        private static void LoadConfiguration()
        {
            string configPath = Path.Combine(AppContext.BaseDirectory, "appCfg.json");
            
            if (!File.Exists(configPath))
            {
                throw new FileNotFoundException(
                    $"Configuration file not found: {configPath}\n" +
                    "Please copy appCfg.example.json to appCfg.json and fill in your test credentials.\n" +
                    "See https://info.mojedatovaschranka.cz/info/cs/74.html for information on creating test accounts."
                );
            }

            string json = File.ReadAllText(configPath);
            _config = JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (_config == null)
            {
                throw new InvalidOperationException("Failed to deserialize configuration file.");
            }

            // Validate that credentials are not empty
            if (string.IsNullOrWhiteSpace(_config.Account1.Username) || 
                string.IsNullOrWhiteSpace(_config.Account1.Password))
            {
                throw new InvalidOperationException(
                    "Account1 credentials are missing in appCfg.json. " +
                    "Please fill in the username and password for your first test account."
                );
            }

            if (string.IsNullOrWhiteSpace(_config.Account2.Username) || 
                string.IsNullOrWhiteSpace(_config.Account2.Password))
            {
                throw new InvalidOperationException(
                    "Account2 credentials are missing in appCfg.json. " +
                    "Please fill in the username and password for your second test account."
                );
            }
        }
    }
}

