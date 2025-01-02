namespace AiMailScanner
{
    public class Config
    {
        public ImapAccessConfig ImapConfig { get; set; } = new ImapAccessConfig();
        public DavAccessConfig CalDavConfig { get; set; } = new DavAccessConfig();
        public string OpenAiSecret { get; set; } = string.Empty;

        public class AccessConfig
        {
            public string UserName { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string Url { get; set; } = string.Empty;
        }

        public class ImapAccessConfig : AccessConfig
        {
            public int Port { get; set; }
        }

        public class DavAccessConfig : AccessConfig
        {
            public string? Calendar { get; set; }
        }
    }
}