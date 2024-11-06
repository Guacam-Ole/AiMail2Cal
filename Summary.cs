using System.Data.SqlTypes;

namespace AiMailScanner
{
    public class Summary
    {
        public DateTime ElementDate { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public string From { get; set; }
    }
}