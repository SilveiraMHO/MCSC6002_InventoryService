using Application.Feature.InventoryItems.Consumers;
using Application.Interfaces;
using Application.Services;
using Application.Services.Interfaces;
using Application.Settings;
using Infrastructure.Context;
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

                    mqConfig.ReceiveEndpoint(rabbitMqSettings.ProductCreatedEventQueue, endpoint =>
                    {
                        endpoint.PrefetchCount = 10;
                        endpoint.UseMessageRetry(retryConfig => retryConfig.Interval(3, TimeSpan.FromMilliseconds(rabbitMqSettings.RetryInMilliseconds)));
                        endpoint.ConfigureConsumer<ProductCreatedEventConsumer>(context);
                    });

                    mqConfig.ReceiveEndpoint(rabbitMqSettings.ProductDeletedEventQueue, endpoint =>
                    {
                        endpoint.PrefetchCount = 10;
                        endpoint.UseMessageRetry(retryConfig => retryConfig.Interval(3, TimeSpan.FromMilliseconds(rabbitMqSettings.RetryInMilliseconds)));
                        endpoint.ConfigureConsumer<ProductDeletedEventConsumer>(context);
                    });
                });
            });
        }
    }
}
