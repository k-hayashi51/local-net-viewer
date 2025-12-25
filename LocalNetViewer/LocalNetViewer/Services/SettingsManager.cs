using LocalNetViewer.Constants;
using LocalNetViewer.Models;
using System;
using System.IO;
using System.Text.Json;

namespace LocalNetViewer.Services
{
    public static class SettingsManager
    {
        private const string filePath = "settings.json";

        public static string Position
        {
            get
            {
                return GetSettings().Position;
            }
            set
            {
                var settings = GetSettings();
                settings.Position = value;
                UpdateSettings(settings);
            }
        }

        public static ImagePageMode ImagePageMode
        {
            get
            {
                return GetSettings().ImagePageMode;
            }
            set
            {
                var settings = GetSettings();
                settings.ImagePageMode = value;
                UpdateSettings(settings);
            }
        }

        private static SettingsModel GetSettings()
        {
            // JSONを読み込む。存在しない場合は空のJSONを作る
            if (!File.Exists(filePath))
            {
                return new SettingsModel();
            }
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<SettingsModel>(json) ?? new SettingsModel();
        }

        private static void UpdateSettings(SettingsModel settings)
        {
            // 書き戻し
            JsonSerializerOptions jsonSerializerOptions = new()
            {
                WriteIndented = true
            };
            var options = jsonSerializerOptions;

            File.WriteAllText(filePath, JsonSerializer.Serialize(settings, options));
        }
    }
}
