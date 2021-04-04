using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json.Linq;

namespace UmamusumeOCR
{
    public interface ITranslator
    {
        public Task<string> TranslateTextAsync(string input);
    }

    public class TranslatorFactory
    {
        public static ITranslator BuildTranslator(string translatorType, string langCode, string configFile)
        {
            var google = new GooglePublicTranslator(langCode);
            try
            {
                ITranslator t = translatorType?.ToLower().Trim() switch
                {
                    "google" => google,
                    "azure" => AzureTranslator.BuildFromConfig(File.ReadAllText(configFile), langCode),
                    _ => google,
                };
                if (t == null)
                {
                    t = google;
                }
                return t;
            }
            catch (Exception)
            {
                return google;
            }
        }
    }

    public class GooglePublicTranslator : ITranslator
    {
        private readonly HttpClient client = new();
        private readonly string Endpoint = "https://translate.googleapis.com/translate_a/";
        private readonly string _landCode;

        public GooglePublicTranslator(string langCode)
        {
            _landCode = langCode;
        }

        public async Task<string> TranslateTextAsync(string input)
        {
            if (input.Trim().Length == 0)
                return "";
            string route = $"single?client=gtx&sl=ja&tl={_landCode}&dt=t&q={Uri.EscapeUriString(input)}";

            try
            {
                using var request = new HttpRequestMessage();

                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(Endpoint + route);

                using var response = client.Send(request);
                string result = await response.Content.ReadAsStringAsync();

                var resultList = JArray.Parse(result)[0].ToArray();
                var sb = new StringBuilder();

                foreach (var line in resultList)
                {
                    sb.AppendLine(line.ToArray()[0].ToString().Trim());
                }
                return sb.ToString();
            } catch (Exception e)
            {
                return "Google Public Translator failed: " + e.Message;
            }
        }
    }

    class AzureTranslator : ITranslator
    {
        private static readonly string SpeakerNameDir = "./Glossaries";

        private readonly TranslatorConifg _config;
        private readonly string _langCode;
        private readonly HttpClient client = new();

        private Dictionary<string, string> _glossaryDictionary = new();

        class TranslatorConifg
        {
            public string Key { get; set; }
            public string Endpoint { get; set; }
            public string Location { get; set; }

            public bool IsValid()
            {
                return !string.IsNullOrEmpty(Key) && !string.IsNullOrEmpty(Endpoint) && !string.IsNullOrEmpty(Location);
            }
        }

        public static AzureTranslator BuildFromConfig(string configJson, string langCode)
        {
            try
            {
                var config = JsonConvert.DeserializeObject<TranslatorConifg>(configJson);
                if (config.IsValid())
                    return new AzureTranslator(config, langCode);
            }
            catch (Exception) { }
            return null;
        }

        private AzureTranslator(TranslatorConifg config, string langCode)
        {
            _config = config;
            _langCode = langCode;
            
            if (!_config.Endpoint.EndsWith('/'))
            {
                _config.Endpoint += "/";
            }
            LoadSpeakerDictionary(langCode);
        }

        private string ApplyGlossaries(string s)
        {
            foreach (var (k, v) in _glossaryDictionary)
            {
                s = s.Replace(k, $"<mstrans:dictionary translation={v}>{k}</mstrans:dictionary>");
            }
            return s;
        }

        private bool LoadSpeakerDictionary(string langCode)
        {
            try
            {
                var path = $"{SpeakerNameDir}/{langCode}.json";
                _glossaryDictionary = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(path));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<string> TranslateTextAsync(string textToTranslate)
        {
            if (textToTranslate.Trim().Length == 0)
                return "";
            string route = $"/translate?api-version=3.0&from=ja&to={_langCode}";
            object[] body = new object[] { new { Text = ApplyGlossaries(textToTranslate) } };
            var requestBody = JsonConvert.SerializeObject(body);

            using var request = new HttpRequestMessage();

            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri(_config.Endpoint + route);
            using var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
            request.Content = content;
            request.Headers.Add("Ocp-Apim-Subscription-Key", _config.Key);
            request.Headers.Add("Ocp-Apim-Subscription-Region", _config.Location);


            using var response = client.Send(request);
            string result = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeObject<List<TranslateResult>>(result).First().translations.First().text;
        }

        class TranslateResult
        {
            public List<TextResult> translations { get; set; }
        }

        class TextResult
        {
            public string text { get; set; }
            public string to { get; set; }
        }
    }


    //public class GoogleTranslator
    //{
    //    private static readonly TranslationClient client = TranslationClient.Create();
    //    private static readonly string projectId = "tts-api-257806";
    //    private static readonly TranslationServiceClient translationServiceClient = TranslationServiceClient.Create();
    //    public static async Task<string> TranslateTextAsync(string s)
    //    {
    //        var response = await client.TranslateTextAsync(
    //            text: s,
    //            targetLanguage: "zh-CN",
    //            sourceLanguage: "ja");
    //        return response.TranslatedText;
    //    }
    //    public static string TranslateTextAsyncV3(string s)
    //    {

    //        TranslateTextRequest request = new()
    //        {
    //            Contents =
    //            {
    //                s
    //            },
    //            TargetLanguageCode = "zh-CN",
    //            SourceLanguageCode = "ja",
    //            ParentAsLocationName = new LocationName(projectId, "us-central1"),
    //        };
    //        TranslateTextResponse response = translationServiceClient.TranslateText(request);
    //        var sb = new StringBuilder();
    //        foreach (Translation translation in response.Translations)
    //        {
    //            sb.AppendLine(translation.TranslatedText);
    //        }
    //        return sb.ToString();
    //    }
    //}
}
