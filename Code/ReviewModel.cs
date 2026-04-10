using System;

namespace GoogleReviewsMonitor.Code
{
    public class ReviewModel
    {
        public string   ReviewId         { get; set; }
        public string   AuthorName       { get; set; }
        public int      StarRating       { get; set; }
        public string   ReviewText       { get; set; }
        public DateTime CreateTime       { get; set; }
        public string   ReviewUrl        { get; set; }

        // Claude AI results
        public string   Sentiment        { get; set; }
        public int      SentimentScore   { get; set; }
        public string   SentimentSummary { get; set; }
        public string   GeneratedReply   { get; set; }

        // Processing status
        public string   Status           { get; set; }
    }
}
