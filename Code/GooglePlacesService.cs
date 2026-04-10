using System;
using System.Collections.Generic;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace GoogleReviewsMonitor.Code
{
    /// <summary>
    /// Phase 1 — Fetches Google reviews using the Places API.
    /// No Google My Business API approval required.
    /// Read-only — cannot post replies (done manually by manager).
    /// </summary>
    public class GooglePlacesService : IDisposable
    {
        private readonly AppSettings _settings;
        private readonly HttpClient  _http;

        private const string BASE_URL = "https://maps.googleapis.com/maps/api/place/details/json";

        public GooglePlacesService(AppSettings settings)
        {
            _settings = settings;
            _http     = new HttpClient();
        }

        // ─────────────────────────────────────────────────────────────────
        // PUBLIC: Fetch all reviews newer than lastChecked
        // ─────────────────────────────────────────────────────────────────
        public List<ReviewModel> GetNewReviewsSince(DateTime lastChecked)
        {
            var results = new List<ReviewModel>();

            string url = $"{BASE_URL}" +
                         $"?place_id={_settings.GooglePlaceId}" +
                         $"&fields=reviews,name,url" +
                         $"&reviews_sort=newest" +
                         $"&key={_settings.GooglePlacesApiKey}";

            var response = _http.GetAsync(url).Result;
            string json  = response.Content.ReadAsStringAsync().Result;

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Places API error {(int)response.StatusCode}: {json}");

            var data   = JObject.Parse(json);
            string status = data["status"]?.ToString();

            if (status != "OK")
                throw new Exception($"Places API returned status: {status}. Check your API key and Place ID.");

            var reviews = data["result"]?["reviews"] as JArray;
            string placeUrl = data["result"]?["url"]?.ToString() ?? "";

            if (reviews == null || reviews.Count == 0)
                return results;

            foreach (var r in reviews)
            {
                // Places API returns time as Unix timestamp
                long unixTime    = r["time"]?.Value<long>() ?? 0;
                DateTime created = DateTimeOffset.FromUnixTimeSeconds(unixTime).UtcDateTime;

                // Skip reviews older than lastChecked
                if (created <= lastChecked) continue;

                var review = new ReviewModel
                {
                    ReviewId         = $"{r["author_name"]}_{unixTime}",
                    AuthorName       = r["author_name"]?.ToString() ?? "Anonymous",
                    StarRating       = r["rating"]?.Value<int>() ?? 0,
                    ReviewText       = r["text"]?.ToString() ?? "",
                    CreateTime       = created,
                    ReviewUrl        = placeUrl,
                    Status           = "Pending"
                };

                results.Add(review);
            }

            return results;
        }

        public void Dispose() => _http?.Dispose();
    }
}
