namespace API_Project.Configuration
{
    public class EmailSettings
    {
        public required string SmtpServer { get; set; }
        public int SmtpPort { get; set; }
        public required string SenderName { get; set; }
        public required string SenderEmail { get; set; }
        public required string SenderPassword { get; set; }
        public bool UseSsl { get; set; }
    }
}
