using System;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace GoogleReviewsMonitor.Code
{
    public class EmailService : IDisposable
    {
        private readonly AppSettings _settings;
        private readonly SmtpClient  _smtp;

        public EmailService(AppSettings settings)
        {
            _settings = settings;
            _smtp = new SmtpClient(settings.SmtpHost, settings.SmtpPort)
            {
                EnableSsl   = true,
                Credentials = new NetworkCredential(settings.SmtpUser, settings.SmtpPass)
            };
        }

        public void SendReviewAlert(ReviewModel review)
        {
            string headerColor    = review.Sentiment == "Positive" ? "#16A34A" :
                                    review.Sentiment == "Negative" ? "#DC2626" : "#D97706";
            string sentimentLabel = review.Sentiment ?? "Unknown";
            string authorName     = review.AuthorName ?? "Anonymous";
            string reviewText     = WebUtility.HtmlEncode(review.ReviewText ?? "");
            string summary        = WebUtility.HtmlEncode(review.SentimentSummary ?? "");
            string reply          = WebUtility.HtmlEncode(review.GeneratedReply ?? "");
            string reviewUrl      = review.ReviewUrl ?? "#";
            string businessName   = _settings.BusinessName ?? "Business";
            string stars          = GetStars(review.StarRating);
            string dateStr        = review.CreateTime.ToString("yyyy-MM-dd HH:mm") + " UTC";
            string subject        = "[" + businessName + "] New " + review.StarRating + "-Star Review - " + sentimentLabel;

            var sb = new StringBuilder();
            sb.Append("<html><body style=\"font-family:Arial,sans-serif;background:#F3F4F6;padding:20px;\">");
            sb.Append("<div style=\"max-width:600px;margin:0 auto;background:white;border-radius:8px;overflow:hidden;\">");

            sb.Append("<div style=\"background:" + headerColor + ";padding:20px;\">");
            sb.Append("<h2 style=\"color:white;margin:0;\">New Google Review - " + sentimentLabel + "</h2>");
            sb.Append("<p style=\"color:rgba(255,255,255,0.85);margin:4px 0 0 0;\">" + businessName + "</p>");
            sb.Append("</div>");

            sb.Append("<div style=\"padding:24px;\">");

            sb.Append("<table style=\"width:100%;border-collapse:collapse;margin-bottom:16px;\">");
            sb.Append("<tr><td style=\"padding:6px 10px;background:#F9FAFB;color:#6B7280;width:100px;\">Reviewer</td><td style=\"padding:6px 10px;font-weight:bold;\">" + authorName + "</td></tr>");
            sb.Append("<tr><td style=\"padding:6px 10px;background:#F9FAFB;color:#6B7280;\">Rating</td><td style=\"padding:6px 10px;\">" + stars + " (" + review.StarRating + "/5)</td></tr>");
            sb.Append("<tr><td style=\"padding:6px 10px;background:#F9FAFB;color:#6B7280;\">Date</td><td style=\"padding:6px 10px;\">" + dateStr + "</td></tr>");
            sb.Append("</table>");

            sb.Append("<div style=\"background:#F9FAFB;border-left:4px solid " + headerColor + ";padding:14px;border-radius:4px;margin-bottom:16px;\">");
            sb.Append("<p style=\"margin:0;font-style:italic;\">\"" + reviewText + "\"</p>");
            sb.Append("</div>");

            sb.Append("<div style=\"background:#EFF6FF;border:1px solid #BFDBFE;padding:14px;border-radius:4px;margin-bottom:16px;\">");
            sb.Append("<p style=\"margin:0 0 6px 0;font-weight:bold;color:#1D4ED8;\">AI ANALYSIS</p>");
            sb.Append("<p style=\"margin:0 0 4px 0;\">Sentiment: " + sentimentLabel + " (Score: " + review.SentimentScore + "/100)</p>");
            sb.Append("<p style=\"margin:0;\">Summary: " + summary + "</p>");
            sb.Append("</div>");

            sb.Append("<div style=\"background:#F0FFF4;border:1px solid #BBF7D0;padding:14px;border-radius:4px;margin-bottom:20px;\">");
            sb.Append("<p style=\"margin:0 0 8px 0;font-weight:bold;color:#15803D;\">SUGGESTED REPLY - Copy and paste this on Google:</p>");
            sb.Append("<p style=\"margin:0;line-height:1.6;\">" + reply + "</p>");
            sb.Append("</div>");

            sb.Append("<div style=\"text-align:center;margin-bottom:16px;\">");
            sb.Append("<a href=\"" + reviewUrl + "\" style=\"background:#4285F4;color:white;padding:12px 24px;border-radius:6px;text-decoration:none;font-weight:bold;\">Open Review on Google</a>");
            sb.Append("</div>");

            sb.Append("<p style=\"color:#9CA3AF;font-size:12px;text-align:center;\">Automated notification from " + businessName + " Reviews Bot.</p>");
            sb.Append("</div></div></body></html>");

            Send(subject, sb.ToString());
        }

        private static string GetStars(int rating)
        {
            string result = "";
            for (int i = 0; i < rating; i++) result += "*";
            for (int i = rating; i < 5; i++) result += "-";
            return result;
        }

        private void Send(string subject, string htmlBody)
        {
            using (var msg = new MailMessage())
            {
                msg.From       = new MailAddress(_settings.SmtpUser, _settings.SenderName);
                msg.To.Add(_settings.AlertEmail);
                msg.Subject    = subject;
                msg.Body       = htmlBody;
                msg.IsBodyHtml = true;
                _smtp.Send(msg);
            }
        }

        public void Dispose() => _smtp?.Dispose();
    }
}
