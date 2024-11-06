using MimeKit;

namespace AiMailScanner
{
    public class Summary
    {
        public DateTime ElementDate { get; set; }
        public string Subject { get; set; }=string.Empty;
        public string Body { get; set; } = string.Empty;
        public InternetAddress? Contact { get; set; } = null; 
    }
}