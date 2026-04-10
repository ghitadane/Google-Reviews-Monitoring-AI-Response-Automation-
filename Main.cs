using System;
using System.Collections.Generic;
using UiPath.CodedWorkflows;
using GoogleReviewsMonitor.Code;

namespace GoogleReviewsMonitor
{
    public partial class Main : CodedWorkflow
    {
        [Workflow]
        public void Execute()
        {
            int processed = 0;
            Log("═══ Google Reviews Monitor — Phase 1 — Starting ═══");

            // ── 1. LOAD CONFIG ────────────────────────────────────────────
            AppSettings settings;
            try
            {
                settings = AppSettings.Load();
                string[] missing = settings.Validate();
                if (missing.Length > 0)
                    Log($"⚠ WARNING — Missing config fields: {string.Join(", ", missing)}");
                else
                    Log("✅ Configuration loaded.");
            }
            catch (Exception ex)
            {
                Log($"FATAL — Cannot load config: {ex.Message}");
                throw;
            }

            // ── 2. GET LAST CHECKED TIMESTAMP ─────────────────────────────
            DateTime lastChecked;
            try
            {
                using (var sheets = new GoogleSheetsService(settings))
                    lastChecked = sheets.GetLastCheckedTime();
                Log($"Last checked: {lastChecked:yyyy-MM-dd HH:mm:ss} UTC");
            }
            catch (Exception ex)
            {
                Log($"⚠ Could not read timestamp — defaulting to 365 days ago: {ex.Message}");
                lastChecked = DateTime.UtcNow.AddDays(-365);
            }

            // ── 3. FETCH NEW REVIEWS ──────────────────────────────────────
            var reviews = new List<ReviewModel>();
            try
            {
                using (var places = new GooglePlacesService(settings))
                    reviews = places.GetNewReviewsSince(lastChecked);
                Log($"✅ Fetched {reviews.Count} new review(s).");
            }
            catch (Exception ex)
            {
                Log($"❌ ERROR fetching reviews: {ex.Message}");
            }

            if (reviews.Count == 0)
                Log("No new reviews found — nothing to process.");

            // ── 4. PROCESS EACH REVIEW ────────────────────────────────────
            foreach (var review in reviews)
            {
                Log($"── Review by {review.AuthorName} ({review.StarRating}★)");

                try
                {
                    // A — Sentiment analysis
                    using (var ai = new ClaudeAIService(settings))
                        review.Sentiment = ai.AnalyzeSentiment(review);
                    Log($"   Sentiment: {review.Sentiment} (score {review.SentimentScore}/100)");

                    // B — Generate reply suggestion
                    using (var ai = new ClaudeAIService(settings))
                        review.GeneratedReply = ai.GenerateReply(review);

                    if (string.IsNullOrWhiteSpace(review.GeneratedReply))
                    {
                        review.Status = "Error-NoReply";
                        Log("   ⚠ Reply generation failed.");
                    }
                    else
                    {
                        review.Status = "PendingManualReply";

                        // C — Email manager with full details + suggested reply
                        try
                        {
                            using (var email = new EmailService(settings))
                                email.SendReviewAlert(review);
                            Log($"   ✅ Email sent to {settings.AlertEmail}");
                        }
                        catch (Exception ex)
                        {
                            Log($"   ⚠ Email failed: {ex.Message}");
                            review.Status = "Error-EmailFailed";
                        }
                    }

                    // D — Log to Google Sheets
                    using (var sheets = new GoogleSheetsService(settings))
                        sheets.LogReview(review);
                    Log($"   ✅ Logged to Sheets — Status: {review.Status}");
                }
                catch (Exception ex)
                {
                    Log($"   ❌ Error: {ex.Message}");
                    review.Status = "Error";
                    try
                    {
                        using (var sheets = new GoogleSheetsService(settings))
                            sheets.LogReview(review);
                    }
                    catch { }
                }

                processed++;
            }

            // ── 5. UPDATE TIMESTAMP ───────────────────────────────────────
            try
            {
                using (var sheets = new GoogleSheetsService(settings))
                    sheets.UpdateLastCheckedTime(DateTime.UtcNow);
                Log("✅ Timestamp updated.");
            }
            catch (Exception ex)
            {
                Log($"⚠ Could not update timestamp: {ex.Message}");
            }

            Log($"═══ Complete — Processed: {processed} review(s) ═══");
        }
    }
}
