using MimeKit;

namespace AiMailScanner
{
    public class EmailContents
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string Subject { get; set; } = string.Empty;
        public string Summary { get; set; } = string.Empty;
        public InternetAddress? Contact { get; set; } = null;
        public string? Location { get; set; }

    }
}