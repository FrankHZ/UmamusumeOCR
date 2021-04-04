using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Windows.Globalization;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;
using Windows.Storage.Streams;
using Newtonsoft.Json;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;

namespace UmamusumeOCR
{
    public interface IOcr
    {
        public abstract Task<string> ExtractTextAsync(Bitmap bitmap, bool combineLines);
    }

    public class OcrFactory
    {
        private static readonly WinRtOCR winRtOcr = new();
        public static IOcr BuildOcr(string ocrType, string ocrConfigFile)
        {
            try
            {
                IOcr ocr = ocrType?.ToLower().Trim() switch
                {
                    "winrtocr" => winRtOcr,
                    "azure" => AzureOCR.BuildFromConfig(File.ReadAllText(ocrConfigFile)),
                    _ => winRtOcr,
                };
                if (ocr == null)
                {
                    ocr = winRtOcr;
                }
                return ocr;
            }
            catch (Exception)
            {
                return winRtOcr;
            }
        }
    }

    public class WinRtOCR : IOcr
    {
        private readonly OcrEngine _winRtOCREngine = OcrEngine.TryCreateFromLanguage(new Language("ja"));

        public async Task<string> ExtractTextAsync(Bitmap bitmap, bool combineLines)
        {
            var text = new StringBuilder();
            using var stream = new InMemoryRandomAccessStream();

            bitmap.Save(stream.AsStream(), ImageFormat.Bmp);
            var decoder = await BitmapDecoder.CreateAsync(stream);
            using var softwareBitmap = await decoder.GetSoftwareBitmapAsync();
            var ocrResult = await _winRtOCREngine.RecognizeAsync(softwareBitmap);

            foreach (var line in ocrResult.Lines)
            {
                if (combineLines)
                {
                    text.Append(Replace(line.Text));
                }
                else
                {
                    text.AppendLine(Replace(line.Text));
                }
            }
            return text.ToString();
        }

        private static string Replace(string s)
        {
            s = s.Replace(" ", "")
                .Replace('?', '？')
                .Replace('!', '！')
                .Replace("/", "ノ")
                .Replace("・・・・・・", "……")
                .Replace("・・・", "…")
                .Replace("・・", "…")
                .Trim('0')
                .Trim('|');
            return s;
        }

    }

    public class AzureOCR : IOcr
    {
        private readonly string endpointOcrAPI = "vision/v3.1/ocr?language=ja&detectOrientation=false";
        private readonly string _subscriptionKey;
        private readonly HttpClient client;

        class OcrConifg
        {
            public string Key { get; set; }
            public string Endpoint { get; set; }

            public bool IsValid()
            {
                return !string.IsNullOrEmpty(Key) && !string.IsNullOrEmpty(Endpoint);
            }
        }

        public static AzureOCR BuildFromConfig(string configJson)
        {
            try
            {
                var config = JsonConvert.DeserializeObject<OcrConifg>(configJson);
                if (config.IsValid())
                    return new AzureOCR(config.Key, config.Endpoint);
            }
            catch (Exception) { }
            return null;
        }

        private AzureOCR(string key, string endpoint)
        {
            _subscriptionKey = key;
            if (!endpoint.EndsWith('/'))
            {
                endpoint += "/";
            }
            client = new()
            {
                BaseAddress = new Uri(endpoint + endpointOcrAPI)
            };
        }

        public async Task<string> ExtractTextAsync(Bitmap bitmap, bool _)
        {
            using var bitmapStream = new MemoryStream();
            bitmap.Save(bitmapStream, ImageFormat.Jpeg);

            using var request = new HttpRequestMessage();

            request.Method = HttpMethod.Post;
            using var content = new ByteArrayContent(bitmapStream.ToArray());
            content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/octet-stream");
            request.Content = content;
            request.Headers.Add("Ocp-Apim-Subscription-Key", _subscriptionKey);

            using var response = client.Send(request);
            string jsonResult = await response.Content.ReadAsStringAsync();

            var sb = new StringBuilder();
            var result = JsonConvert.DeserializeObject<OcrResponse>(jsonResult);

            foreach (var r in result.Regions)
            {
                foreach (var l in r.Lines)
                {
                    l.Words.Select(w => w.Text).ToList().ForEach(s => sb.Append(s.Trim('0')));
                    sb.AppendLine();
                }
            }
            return sb.ToString();
        }

        public class OcrResponse
        {
            public string Language { get; set; }
            public double TextAngle { get; set; }
            public string Orientation { get; set; }
            public List<Region> Regions { get; set; }
        }

        public class Region
        {
            public string BoundingBox { get; set; }
            public List<Line> Lines { get; set; }
        }

        public class Line
        {
            public string BoundingBox { get; set; }
            public List<Word> Words { get; set; }
        }

        public class Word
        {
            public string BoundingBox { get; set; }
            public string Text { get; set; }
        }
    }
}
