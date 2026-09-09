namespace GradesManager.Configuration
{
    public sealed class NotificationServiceOptions
    {
        public const string SectionName = "NotificationService";

        public string BaseUrl { get; set; } = "http://localhost:5101";
    }
}
