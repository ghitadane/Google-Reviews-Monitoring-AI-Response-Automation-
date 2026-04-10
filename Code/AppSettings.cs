using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace GoogleReviewsMonitor.Code
{
    public class AppSettings
    {
        // ── Claude AI ─────────────────────────────────────────────────────
        public string ClaudeApiKey  { get; set; }
        public string ClaudeModel   { get; set; }

        // ── Google Places API (Phase 1 — no approval needed) ──────────────
        public string GooglePlacesApiKey { get; set; }
        public string GooglePlaceId      { get; set; }

        // ── Google Sheets ─────────────────────────────────────────────────
        public string GoogleSheetsId         { get; set; }
        public string GoogleSheetsTabName    { get; set; }
        public string ServiceAccountJsonPath { get; set; }

        // ── Gmail SMTP ────────────────────────────────────────────────────
        public string SmtpHost   { get; set; }
        public int    SmtpPort   { get; set; }
        public string SmtpUser   { get; set; }
        public string SmtpPass   { get; set; }
        public string AlertEmail { get; set; }
        public string SenderName { get; set; }

        // ── Workflow ──────────────────────────────────────────────────────
        public string BusinessName           { get; set; }
        public int    NegativeScoreThreshold { get; set; }

        public static AppSettings Load(string filePath = null)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                if (!File.Exists(filePath))
                    filePath = Path.Combine(Directory.GetCurrentDirectory(), "appsettings.json");
            }

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"appsettings.json not found at: {filePath}");

            var settings = JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(filePath));
            if (settings == null)
                throw new InvalidOperationException("appsettings.json is invalid JSON.");

            // Defaults
            if (string.IsNullOrEmpty(settings.ClaudeModel))
                settings.ClaudeModel = "claude-sonnet-4-20250514";
            if (string.IsNullOrEmpty(settings.GoogleSheetsTabName))
                settings.GoogleSheetsTabName = "ReviewsLog";
            if (settings.SmtpPort <= 0)
                settings.SmtpPort = 587;
            if (string.IsNullOrEmpty(settings.SmtpHost))
                settings.SmtpHost = "smtp.gmail.com";
            if (settings.NegativeScoreThreshold <= 0)
                settings.NegativeScoreThreshold = 40;
            if (string.IsNullOrEmpty(settings.SenderName))
                settings.SenderName = "Google Reviews Bot";

            return settings;
        }

        public string[] Validate()
        {
            var missing = new List<string>();
            if (string.IsNullOrWhiteSpace(ClaudeApiKey))          missing.Add("ClaudeApiKey");
            if (string.IsNullOrWhiteSpace(GooglePlacesApiKey))    missing.Add("GooglePlacesApiKey");
            if (string.IsNullOrWhiteSpace(GooglePlaceId))         missing.Add("GooglePlaceId");
            if (string.IsNullOrWhiteSpace(GoogleSheetsId))        missing.Add("GoogleSheetsId");
            if (string.IsNullOrWhiteSpace(ServiceAccountJsonPath))missing.Add("ServiceAccountJsonPath");
            if (string.IsNullOrWhiteSpace(SmtpUser))              missing.Add("SmtpUser");
            if (string.IsNullOrWhiteSpace(SmtpPass))              missing.Add("SmtpPass");
            if (string.IsNullOrWhiteSpace(AlertEmail))            missing.Add("AlertEmail");
            if (string.IsNullOrWhiteSpace(BusinessName))          missing.Add("BusinessName");
            return missing.ToArray();
        }
    }
}
