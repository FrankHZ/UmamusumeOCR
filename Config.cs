using System;
using System.Drawing;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace UmamusumeOCR
{
    public class Config
    {
        public static readonly Rectangle DefaultWindow = new()
        {
            X = 1200,
            Y = 80,
            Width = 1106,
            Height = 1991
        };

        public static readonly Rectangle DefaultGameArea = new()
        {
            X = 14,
            Y = 57,
            Width = 1077,
            Height = 1921
        };

        public static readonly string DefaultWindowTitle = "umamusume";

        private static readonly string ConfigPath = "./Config.json";

        public static bool IsDefaultSize(Size size)
        {
            return size.Width == DefaultGameArea.Width && size.Height == DefaultGameArea.Height;
        }


        public static readonly string[] AvaliableLanguages = { "zh-Hans", "en" };
        public static readonly string DefaultLanguage = "en";
        public string Language { get; set; } = DefaultLanguage;
        public Rectangle WindowArea { get; set; } = DefaultWindow;
        public Rectangle GameArea { get; set; } = DefaultGameArea;
        public string WindowTitle { get; set; } = DefaultWindowTitle;

        public string Ocr { get; set; }
        public string OcrConfig { get; set; }
        private static string defaultOcr = "winrtOCR";
        public string Translator { get; set; }
        public string TranslatorConfig { get; set; }
        private static string defaultTranslator = "google";
        public static Config LoadGameConfig()
        {
            Config config;
            if (File.Exists(ConfigPath))
            {
                try
                {
                    config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(ConfigPath));
                    if (!AvaliableLanguages.Contains(config.Language))
                    {
                        config.Language = DefaultLanguage;
                    }
                    return config;
                }
                catch (Exception) { }
            }
            config = new Config()
            {
                Translator = defaultTranslator,
                Ocr = defaultOcr
            };
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(config));
            return config;
        }

        public void SaveConfig()
        {
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(this));
        }
    }
}
