using Application.Settings;
using MassTransit;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Messaging
{
    public class InvalidMessageSignatureException(string message) : Exception(message);

    // Before every consumer: unsigned, tampered or stale messages go to <queue>_error, no retry.
    public class VerifySignatureFilter<T>(IOptions<RabbitMqSettings> rabbitMqSettings, ILogger<VerifySignatureFilter<T>> logger)
        : IFilter<ConsumeContext<T>> where T : class
    {
        // Limitation: replays inside the window pass; dedupe by MessageId if needed.
        private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(5);

        private readonly byte[] _signingKey = MessageSignature.ParseKey(rabbitMqSettings.Value.SigningKey);

        public Task Send(ConsumeContext<T> context, IPipe<ConsumeContext<T>> next)
        {
            var signature = context.Headers.Get<string>(MessageSignature.SignatureHeader);
            var signedAt = context.Headers.Get<string>(MessageSignature.SignedAtHeader);

            var reason =
                signature is null || signedAt is null ? "missing signature"
                : !long.TryParse(signedAt, out var seconds) || (DateTimeOffset.UtcNow - DateTimeOffset.FromUnixTimeSeconds(seconds)).Duration() > MaxAge ? "expired or invalid timestamp"
                : !CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(MessageSignature.Compute(context.Message, signedAt, _signingKey)),
                    Encoding.ASCII.GetBytes(signature)) ? "signature mismatch"
                : null;

            if (reason is not null)
            {
                logger.LogWarning("Rejected {MessageType} {MessageId}: {Reason}", typeof(T).Name, context.MessageId, reason);
                throw new InvalidMessageSignatureException($"{typeof(T).Name} rejected: {reason}");
            }

            return next.Send(context);
        }

        public void Probe(ProbeContext context) => context.CreateFilterScope("verifySignature");
    }
}
