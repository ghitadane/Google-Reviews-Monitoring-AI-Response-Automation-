════════════════════════════════════════════════
  GoogleReviewsMonitor — PHASE 1
  Google Reviews AI Sentiment Auto-Reply System
  Automation Consultant, Kaunas
  Business Digitalization Management — KTU 2026
════════════════════════════════════════════════
 
BEFORE RUNNING — fill in appsettings.json:
 
  ┌─────────────────────────────────────────────────────────────┐
  │ KEY                    │ WHERE TO GET IT                     │
  ├─────────────────────────────────────────────────────────────┤
  │ ClaudeApiKey           │ console.anthropic.com               │
  │                        │ → Sign in → API Keys → Create Key  │
  │                        │ Key starts with: sk-ant-api03-...  │
  ├─────────────────────────────────────────────────────────────┤
  │ GooglePlacesApiKey     │ console.cloud.google.com            │
  │                        │ → APIs & Services → Credentials     │
  │                        │ → Create API Key                    │
  │                        │ (Enable Places API first)           │
  ├─────────────────────────────────────────────────────────────┤
  │ GooglePlaceId          │ maps.googleapis.com/maps/api/place  │
  │                        │ /findplacefromtext/json?input=      │
  │                        │ YOUR+BUSINESS+NAME&key=YOUR_KEY     │
  │                        │ Look for "place_id" in the result   │
  ├─────────────────────────────────────────────────────────────┤
  │ GoogleSheetsId         │ Open your Google Sheet              │
  │                        │ Copy the ID from the URL:           │
  │                        │ docs.google.com/spreadsheets/d/     │
  │                        │ → THIS_PART_IS_YOUR_ID ←           │
  ├─────────────────────────────────────────────────────────────┤
  │ ServiceAccountJsonPath │ console.cloud.google.com            │
  │                        │ → IAM & Admin → Service Accounts    │
  │                        │ → Create Service Account            │
  │                        │ → Keys → Add Key → JSON → Download  │
  │                        │ Share your Google Sheet with the    │
  │                        │ service account email as Editor     │
  │                        │ Put the file in credentials/ folder │
  ├─────────────────────────────────────────────────────────────┤
  │ SmtpUser               │ Your Gmail address                  │
  │                        │ e.g. yourname@gmail.com             │
  ├─────────────────────────────────────────────────────────────┤
  │ SmtpPass               │ myaccount.google.com/security       │
  │                        │ → 2-Step Verification → ON first    │
  │                        │ → App Passwords → Create            │
  │                        │ → Select "Mail" → Generate          │
  │                        │ 16-character password (no spaces)   │
  └─────────────────────────────────────────────────────────────┘
 
HOW IT WORKS (Phase 1):
 
  1. Bot fetches new reviews from Google via Places API
  2. Claude AI analyses sentiment (Positive / Neutral / Negative)
     and generates a confidence score (0–100)
  3. Claude generates a personalised suggested reply
  4. You receive an email alert containing:
       - The review text and star rating
       - Sentiment analysis result and score
       - AI-generated suggested reply
       - Link to open Google Business and post it
  5. You copy the reply and post it manually on Google
  6. Everything is logged to Google Sheets automatically
     Live log: https://docs.google.com/spreadsheets/d/
               1rrSW-JNwvnfElAsSGJ-Ks6Pwcdop-sHRE_vn-0ZdlNA
 
HOW TO RUN IN UIPATH STUDIO:
 
  1. Open UiPath Studio
  2. File → Open → select this project folder
  3. Wait for NuGet packages to restore automatically
  4. Fill in credentials/appsettings.json with your keys
  5. Press F5 to run
  6. Check Output panel for live logs
  7. Check your Gmail inbox for the email alert
  8. Check Google Sheets ReviewsLog tab for the logged data
 
SCHEDULE (optional — Phase 2):
 
  Windows Task Scheduler → every 30 minutes (recommended)
  UiPath Orchestrator → Time Trigger → every 30 minutes
 
FOLDER STRUCTURE:
 
  GoogleReviewsMonitor/
  ├── Main.cs                          ← Entry point
  ├── Code/
  │   ├── ClaudeAIService.cs           ← Claude AI integration
  │   ├── GooglePlacesService.cs       ← Fetch reviews
  │   ├── GoogleSheetsService.cs       ← Logging
  │   ├── EmailService.cs              ← Gmail alerts
  │   └── AppSettings.cs              ← Config loader
  ├── credentials/
  │   ├── appsettings.json             ← YOUR KEYS (gitignored)
  │   ├── appsettings.example.json     ← Template (safe to share)
  │   └── sheets-service-account.json ← Google service account
  └── README.txt                       ← This file
 
TROUBLESHOOTING:
 
  Claude API 401 error    → API key expired, generate a new one
                            at console.anthropic.com
  Sheets JSON not found   → Check ServiceAccountJsonPath in
                            appsettings.json uses double backslashes
                            e.g. C:\\Users\\name\\credentials\\file.json
  Gmail SMTP error        → Regenerate App Password (16 chars)
                            Make sure 2FA is enabled on Gmail first
  No reviews fetched      → Verify GooglePlaceId is correct
  Sheets write error      → Share the spreadsheet with the
                            service account email as Editor
 
TEAM:
 
  RPA Developer : Zinedine Benmiya (z.benmiya@gmail.com)
  Documentation : [Teammate 2] | [Teammate 3] | [Teammate 4]
  Programme     : Business Digitalization Management — KTU
  Course        : Intelligent Process Automation — S190B191
 
════════════════════════════════════════════════
