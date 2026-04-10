using System;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GoogleReviewsMonitor.Code
{
    public class ClaudeAIService : IDisposable
    {
        private readonly HttpClient _http;
        private readonly string     _apiKey;
        private readonly string     _model;

        private const string API_URL     = "https://api.anthropic.com/v1/messages";
        private const string API_VERSION = "2023-06-01";

        public ClaudeAIService(AppSettings settings)
        {
            _apiKey = settings.ClaudeApiKey
                ?? throw new ArgumentNullException("ClaudeApiKey missing in appsettings.json");
            _model  = settings.ClaudeModel ?? "claude-sonnet-4-20250514";

            _http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            _http.DefaultRequestHeaders.Add("x-api-key", _apiKey);
            _http.DefaultRequestHeaders.Add("anthropic-version", API_VERSION);
        }

        public string AnalyzeSentiment(ReviewModel review)
        {
            string system =
                "You are a customer review sentiment analyzer. " +
                "Respond ONLY with a JSON object — no markdown, no explanation. " +
                "Format exactly: {\"label\":\"Positive\",\"score\":85,\"summary\":\"one sentence here\"}. " +
                "label must be exactly Positive, Neutral, or Negative. score is 0-100.";

            string user =
                $"Analyze this Google Business review:\n" +
                $"Star rating: {review.StarRating}/5\n" +
                $"Review: \"{review.ReviewText}\"";

            string raw = CallClaude(system, user, 150);

            try
            {
                var json = JObject.Parse(StripMarkdown(raw));
                string lbl = json["label"]?.ToString() ?? "Neutral";
                review.SentimentScore   = json["score"]?.Value<int>() ?? 50;
                review.SentimentSummary = json["summary"]?.ToString() ?? "";

                if (lbl.IndexOf("Pos", StringComparison.OrdinalIgnoreCase) >= 0) return "Positive";
                if (lbl.IndexOf("Neg", StringComparison.OrdinalIgnoreCase) >= 0) return "Negative";
                return "Neutral";
            }
            catch
            {
                review.SentimentScore   = 50;
                review.SentimentSummary = "Could not parse sentiment.";
                return "Neutral";
            }
        }

        public string GenerateReply(ReviewModel review)
        {
            string system =
                $"You are the owner of {_model} writing a genuine professional reply to a Google review. " +
                "Write 2-3 sentences maximum. Be warm and specific. " +
                "Do not start with Dear. Output only the reply text.";

            string user =
                $"Write a reply to this {review.Sentiment} review.\n" +
                $"Reviewer: {review.AuthorName}\n" +
                $"Rating: {review.StarRating}/5\n" +
                $"Review: \"{review.ReviewText}\"\n" +
                $"Summary: {review.SentimentSummary}";

            return CallClaude(system, user, 200)?.Trim() ?? string.Empty;
        }

        private string CallClaude(string system, string user, int maxTokens)
        {
            int retries = 3;
            for (int i = 1; i <= retries; i++)
            {
                try
                {
                    var body = new
                    {
                        model      = _model,
                        max_tokens = maxTokens,
                        system     = system,
                        messages   = new[] { new { role = "user", content = user } }
                    };

                    var content  = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
                    var response = _http.PostAsync(API_URL, content).Result;
                    string json  = response.Content.ReadAsStringAsync().Result;

                    if (!response.IsSuccessStatusCode)
                        throw new Exception($"Claude API {(int)response.StatusCode}: {json}");

                    return JObject.Parse(json)["content"]?[0]?["text"]?.ToString() ?? string.Empty;
                }
                catch (Exception ex)
                {
                    if (i == retries) throw new Exception($"Claude API failed after {retries} attempts: {ex.Message}");
                    System.Threading.Thread.Sleep(TimeSpan.FromSeconds(5 * i));
                }
            }
            return string.Empty;
        }

        private static string StripMarkdown(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return "{}";
            raw = raw.Trim();
            if (raw.StartsWith("```"))
            {
                int nl   = raw.IndexOf('\n');
                int last = raw.LastIndexOf("```");
                if (nl >= 0 && last > nl)
                    raw = raw.Substring(nl + 1, last - nl - 1).Trim();
            }
            return raw;
        }

        public void Dispose() => _http?.Dispose();
    }
}
