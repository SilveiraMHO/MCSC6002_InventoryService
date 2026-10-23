using Application.Feature.InventoryItems.Consumers;
using Application.Interfaces;
using Application.Services;
using Application.Services.Interfaces;
using Application.Settings;
using Infrastructure.Context;
using Infrastructure.Messaging;
using Infrastructure.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration config)
        {
            services.AddDatabase(config);

            services
                .AddScoped<IInventoryItemService, InventoryItemService>()
                .AddScoped<IInventoryItemRepository, InventoryItemRepository>();

            return services;
        }

        public static async Task MigrateDatabaseAsync(this IServiceProvider services)
        {
            using var scope = services.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync(); // Creates the DB on a fresh server.
        }

        private static IServiceCollection AddDatabase(this IServiceCollection services, IConfiguration config)
        {
            return services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(config.GetConnectionString("DefaultConnection"), builder =>
                {
                    builder.MigrationsHistoryTable("Migrations", "EFCore");
                })
            );
        }

        public static void AddRabbitMQServiceConfiguration(this IServiceCollection services, RabbitMqSettings rabbitMqSettings)
        {
            MessageSignature.ParseKey(rabbitMqSettings.SigningKey); // Fail fast at startup.
            var ca = X509CertificateLoader.LoadCertificateFromFile(rabbitMqSettings.CaCertificatePath);

            // Invalid messages: no retry, straight to _error.
            void Retry(IRetryConfigurator retryConfig)
            {
                retryConfig.Interval(3, TimeSpan.FromMilliseconds(rabbitMqSettings.RetryInMilliseconds));
                retryConfig.Ignore<InvalidMessageSignatureException>();
                retryConfig.Ignore<ArgumentOutOfRangeException>();
            }

            services.AddMassTransit(config =>
            {
                config.AddConsumer<ProductCreatedEventConsumer>();
                config.AddConsumer<ProductDeletedEventConsumer>();

                config.UsingRabbitMq((context, mqConfig) =>
                {
                    mqConfig.Host(rabbitMqSettings.Host, rabbitMqSettings.Port, "/", host =>
                    {
                        host.Username(rabbitMqSettings.UserName);
                        host.Password(rabbitMqSettings.Password);
                        host.UseSsl(ssl =>
                        {
                            ssl.ServerName = rabbitMqSettings.ServerName;
                            ssl.Protocol = SslProtocols.Tls12 | SslProtocols.Tls13;
                            ssl.CertificateValidationCallback = (_, certificate, _, errors) => IsIssuedByCa(certificate, errors, ca);
                        });
                    });

                    mqConfig.UseConsumeFilter(typeof(VerifySignatureFilter<>), context); // HMAC check before every consumer.

                    mqConfig.ReceiveEndpoint(rabbitMqSettings.ProductCreatedEventQueue, endpoint =>
                    {
                        endpoint.PrefetchCount = 10;
                        endpoint.UseMessageRetry(Retry);
                        endpoint.ConfigureConsumer<ProductCreatedEventConsumer>(context);
                    });

                    mqConfig.ReceiveEndpoint(rabbitMqSettings.ProductDeletedEventQueue, endpoint =>
                    {
                        endpoint.PrefetchCount = 10;
                        endpoint.UseMessageRetry(Retry);
                        endpoint.ConfigureConsumer<ProductDeletedEventConsumer>(context);
                    });
                });
            });
        }

        // Broker cert: project CA only.
        private static bool IsIssuedByCa(X509Certificate certificate, SslPolicyErrors errors, X509Certificate2 ca)
        {
            if (certificate is not X509Certificate2 brokerCertificate || errors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch))
            {
                return false;
            }

            using var chain = new X509Chain();
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust;
            chain.ChainPolicy.CustomTrustStore.Add(ca);
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
            chain.ChainPolicy.ApplicationPolicy.Add(new Oid("1.3.6.1.5.5.7.3.1")); // serverAuth

            return chain.Build(brokerCertificate);
        }
    }
}
