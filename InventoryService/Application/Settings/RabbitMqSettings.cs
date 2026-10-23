namespace Application.Settings
{
    public class RabbitMqSettings
    {
        public string Host { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string SigningKey { get; set; } // Base64 HMAC key shared with ProductService.
        public string ProductCreatedEventQueue { get; set; }
        public string ProductDeletedEventQueue { get; set; }
        public int RetryInMilliseconds { get; set; }
    }
}
