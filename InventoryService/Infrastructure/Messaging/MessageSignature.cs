using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Messaging
{
    // HMAC-SHA256 over "event type | timestamp | JSON body". ProductService signs with the same scheme;
    // this service rejects messages edited in RabbitMQ or published without the shared key.
    // The type is included so a signature for one event type can't be reused for another.
    public static class MessageSignature
    {
        public const string SignatureHeader = "X-Signature";
        public const string SignedAtHeader = "X-Signed-At";

        public static string Compute<T>(T message, string signedAt, byte[] key)
        {
            var payload = $"{typeof(T).FullName}|{signedAt}|{JsonSerializer.Serialize(message)}";

            return Convert.ToBase64String(HMACSHA256.HashData(key, Encoding.UTF8.GetBytes(payload)));
        }

        public static byte[] ParseKey(string base64Key)
        {
            var key = string.IsNullOrEmpty(base64Key) ? [] : Convert.FromBase64String(base64Key);

            return key.Length >= 32
                ? key
                : throw new InvalidOperationException("RabbitMqSettings:SigningKey must be a base64 key of at least 32 bytes (set it with dotnet user-secrets).");
        }
    }
}
