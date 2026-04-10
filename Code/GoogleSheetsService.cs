using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GoogleReviewsMonitor.Code
{
    public class GoogleSheetsService : IDisposable
    {
        private readonly AppSettings _settings;
        private readonly HttpClient  _http;

        private const string SHEETS_BASE = "https://sheets.googleapis.com/v4/spreadsheets";
        private const string TOKEN_URL   = "https://oauth2.googleapis.com/token";
        private const string SCOPE       = "https://www.googleapis.com/auth/spreadsheets";

        public GoogleSheetsService(AppSettings settings)
        {
            _settings = settings;
            _http     = new HttpClient();
            string token = GetServiceAccountToken();
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }

        public void LogReview(ReviewModel review)
        {
            string tab  = _settings.GoogleSheetsTabName ?? "ReviewsLog";
            string url  = $"{SHEETS_BASE}/{_settings.GoogleSheetsId}/values/{tab}:append" +
                          "?valueInputOption=USER_ENTERED&insertDataOption=INSERT_ROWS";

            string text = review.ReviewText ?? "";
            if (text.Length > 500) text = text.Substring(0, 497) + "...";

            var row = new[]
            {
                DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                review.CreateTime.ToString("yyyy-MM-dd"),
                review.AuthorName ?? "",
                review.StarRating.ToString(),
                text,
                review.Sentiment ?? "",
                review.SentimentScore.ToString(),
                review.SentimentSummary ?? "",
                review.GeneratedReply ?? "",
                review.Status ?? "",
                review.ReviewUrl ?? ""
            };

            var body     = new { values = new[] { row } };
            var content  = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            var response = _http.PostAsync(url, content).Result;

            if (!response.IsSuccessStatusCode)
            {
                string err = response.Content.ReadAsStringAsync().Result;
                throw new Exception($"Sheets append failed: {err}");
            }
        }

        public DateTime GetLastCheckedTime()
        {
            try
            {
                string url   = $"{SHEETS_BASE}/{_settings.GoogleSheetsId}/values/Config!B1";
                var response = _http.GetAsync(url).Result;
                string json  = response.Content.ReadAsStringAsync().Result;
                var data     = JObject.Parse(json);
                string val   = data["values"]?[0]?[0]?.ToString();

                if (!string.IsNullOrEmpty(val) && DateTime.TryParse(val, out DateTime dt))
                    return dt.ToUniversalTime();
            }
            catch { }

            return DateTime.UtcNow.AddDays(-30);
        }

        public void UpdateLastCheckedTime(DateTime time)
        {
            // Write label to A1
            try
            {
                string labelUrl = $"{SHEETS_BASE}/{_settings.GoogleSheetsId}/values/Config!A1?valueInputOption=USER_ENTERED";
                var labelBody   = new { values = new[] { new[] { "LastCheckedUTC" } } };
                var labelContent = new StringContent(JsonConvert.SerializeObject(labelBody), Encoding.UTF8, "application/json");
                _http.PutAsync(labelUrl, labelContent).Wait();
            }
            catch { }

            // Write timestamp to B1
            string url     = $"{SHEETS_BASE}/{_settings.GoogleSheetsId}/values/Config!B1?valueInputOption=USER_ENTERED";
            var body       = new { values = new[] { new[] { time.ToString("o") } } };
            var content    = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            var response   = _http.PutAsync(url, content).Result;

            if (!response.IsSuccessStatusCode)
            {
                string err = response.Content.ReadAsStringAsync().Result;
                throw new Exception($"Sheets timestamp update failed: {err}");
            }
        }

        private string GetServiceAccountToken()
        {
            string path = _settings.ServiceAccountJsonPath;
            if (!File.Exists(path))
                throw new FileNotFoundException($"Service account JSON not found: {path}");

            var sa       = JObject.Parse(File.ReadAllText(path));
            string email = sa["client_email"].ToString();
            string key   = sa["private_key"].ToString();
            string jwt   = BuildJwt(email, key);

            var body     = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("grant_type", "urn:ietf:params:oauth:grant-type:jwt-bearer"),
                new KeyValuePair<string,string>("assertion",  jwt),
            });

            var response = _http.PostAsync(TOKEN_URL, body).Result;
            string json  = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Service account token failed: {json}");

            return JObject.Parse(json)["access_token"].ToString();
        }

        private string BuildJwt(string clientEmail, string privateKeyPem)
        {
            long now    = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var header  = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new { alg = "RS256", typ = "JWT" })));
            var payload = Base64UrlEncode(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(new
            {
                iss   = clientEmail,
                scope = SCOPE,
                aud   = TOKEN_URL,
                iat   = now,
                exp   = now + 3600
            })));

            string input = $"{header}.{payload}";
            var rsa      = System.Security.Cryptography.RSA.Create();
            string pem   = privateKeyPem
                .Replace("-----BEGIN PRIVATE KEY-----", "")
                .Replace("-----END PRIVATE KEY-----", "")
                .Replace("\n", "").Replace("\r", "").Trim();
            rsa.ImportPkcs8PrivateKey(Convert.FromBase64String(pem), out _);

            byte[] sig = rsa.SignData(
                Encoding.UTF8.GetBytes(input),
                System.Security.Cryptography.HashAlgorithmName.SHA256,
                System.Security.Cryptography.RSASignaturePadding.Pkcs1);

            return $"{input}.{Base64UrlEncode(sig)}";
        }

        private static string Base64UrlEncode(byte[] data) =>
            Convert.ToBase64String(data).Replace('+', '-').Replace('/', '_').TrimEnd('=');

        public void Dispose() => _http?.Dispose();
    }
}
