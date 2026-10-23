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
                    mqConfig.Host(rabbitMqSettings.Host, "/", host =>
                    {
                        host.Username(rabbitMqSettings.UserName);
                        host.Password(rabbitMqSettings.Password);
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
    }
}
