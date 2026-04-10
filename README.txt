════════════════════════════════════════════════
  GoogleReviewsMonitor — PHASE 1
  Automation Consultant, Kaunas
════════════════════════════════════════════════

BEFORE RUNNING — fill in appsettings.json:
  ClaudeApiKey  → your new Claude API key
  SmtpPass      → your ReviewsBot Gmail App Password

EVERYTHING ELSE IS PRE-FILLED ✅

HOW IT WORKS (Phase 1):
  1. Bot fetches new reviews from Google via Places API
  2. Claude AI analyzes sentiment (Positive/Neutral/Negative)
  3. Claude generates a suggested reply
  4. You receive an email with:
     - The review
     - Sentiment analysis
     - Suggested reply
     - Button to open Google and post it
  5. You copy the reply and post it manually on Google
  6. Everything is logged to Google Sheets automatically

HOW TO RUN IN UIPATH STUDIO:
  1. Open Studio → File → Open → select this folder
  2. Wait for NuGet packages to restore
  3. Press F5 to run

SCHEDULE (optional):
  In UiPath Orchestrator → Time Trigger → every 2 hours
════════════════════════════════════════════════
