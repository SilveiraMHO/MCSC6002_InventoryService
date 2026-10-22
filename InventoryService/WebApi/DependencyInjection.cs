using Application.Settings;
using AuthLibrary.Constants.Authentication;
using AuthLibrary.Permissions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;
using ResponseLibrary.Wrappers;
using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using WebApi.Middleware;

namespace WebApi
{
    public static class DependencyInjection
    {
        internal static IServiceCollection AddIdentitySettings(this IServiceCollection services)
        {
            services
                .AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>()
                .AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();

            return services;
        }

        internal static TokenSettings GetTokenSettings(this IServiceCollection services, IConfiguration config)
        {
            var tokenSettingsConfig = config.GetSection(nameof(TokenSettings));

            services.Configure<TokenSettings>(tokenSettingsConfig);

            return tokenSettingsConfig.Get<TokenSettings>();
        }

        internal static RabbitMqSettings GetRabbitMqSettings(this IServiceCollection services, IConfiguration config)
        {
            var rabbitMqSettingsConfig = config.GetSection(nameof(RabbitMqSettings));

            services.Configure<RabbitMqSettings>(rabbitMqSettingsConfig);

            return rabbitMqSettingsConfig.Get<RabbitMqSettings>();
        }

        internal static IServiceCollection AddJwtAuthentication(this IServiceCollection services, TokenSettings tokenSettings)
        {
            // RS256: this service only has the public key, so it can verify tokens but not create them.
            var rsa = RSA.Create();
            rsa.ImportFromPem(File.ReadAllText(tokenSettings.PublicKeyPath));

            services
                .AddAuthentication(auth =>
                {
                    auth.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    auth.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(bearer =>
                {
                    bearer.RequireHttpsMetadata = false;
                    bearer.SaveToken = true; // After login, the token will be attached in AuthenticationProperties.
                    bearer.TokenValidationParameters = new TokenValidationParameters // Those are the parameters that JWT will validate in the token provided on the header of the request.
                    {
                        ValidateIssuerSigningKey = true,
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidIssuer = AppClaim.Issuer,
                        ValidAudience = AppClaim.Audience,
                        RoleClaimType = ClaimTypes.Role,
                        ClockSkew = TimeSpan.Zero,
                        IssuerSigningKey = new RsaSecurityKey(rsa),
                        ValidAlgorithms = [SecurityAlgorithms.RsaSha256]
                    };

                    // If the validations above failed for some how, it will happen:
                    bearer.Events = new JwtBearerEvents
                    {
                        OnAuthenticationFailed = context =>
                        {
                            if (context.Exception is SecurityTokenExpiredException)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                context.Response.ContentType = "application/json";

                                var result = JsonSerializer.Serialize(ResponseWrapper.Fail("The token is expired."));

                                return context.Response.WriteAsync(result);
                            }
                            else
                            {
                                // Bad signature, wrong algorithm, malformed token: client error, not server error.
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                context.Response.ContentType = "application/json";

                                var result = JsonSerializer.Serialize(ResponseWrapper.Fail("Invalid token."));

                                return context.Response.WriteAsync(result);
                            }
                        },
                        OnChallenge = context =>
                        {
                            context.HandleResponse();

                            if (!context.Response.HasStarted)
                            {
                                context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                                context.Response.ContentType = "application/json";

                                var result = JsonSerializer.Serialize(ResponseWrapper.Fail("You are not Authorized."));

                                return context.Response.WriteAsync(result);
                            }

                            return Task.CompletedTask;
                        },
                        OnForbidden = context =>
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                            context.Response.ContentType = "application/json";

                            var result = JsonSerializer.Serialize(ResponseWrapper.Fail("You are not authorized to access this resource."));

                            return context.Response.WriteAsync(result);
                        }
                    };
                });

            // Config to dynamically add the permissions (that are defined in AppPermissions) into the authorization options.
            // With this approach, we do not need to check in Database the permission of the user that is logining, the permissions stay in memory. 
            services.
                AddAuthorization(options =>
                {
                    foreach (var prop in typeof(AppPermissions).GetNestedTypes()
                        .SelectMany(c => c.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)))
                    {
                        var propertyValue = prop.GetValue(null);

                        if (propertyValue is not null)
                        {
                            options.AddPolicy(propertyValue.ToString(), policy => policy.RequireClaim(AppClaim.Permission, propertyValue.ToString()));
                        }
                    }
                });

            return services;
        }

        internal static void RegisterInventoryServiceClient(this IServiceCollection services, IConfiguration config)
        {
            var inventoryServiceClientSettings = config.GetSection(nameof(InventoryServiceClientSettings)).Get<InventoryServiceClientSettings>();
            
            services.AddHttpClient("ProductServiceClient", client =>
            {
                client.BaseAddress = new Uri(inventoryServiceClientSettings.ApiGatewayUrl);
            })
            .AddHttpMessageHandler<TokenForwardingHandler>()
            .AddPolicyHandler(GetRetryPolicy());

            services.AddTransient<TokenForwardingHandler>();
            services.AddHttpContextAccessor();
        }

        private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    retryCount: 3,
                    sleepDurationProvider: retryAttempt =>
                    {
                        double waitTime = Math.Pow(2, retryAttempt) * 100;
                        return TimeSpan.FromMilliseconds(waitTime);
                    },
                    onRetry: (outcome, timespan, retryAttempt, context) =>
                    {
                        // Log the retry attempt
                        Console.WriteLine($"Retrying... Attempt {retryAttempt} after {timespan.TotalMilliseconds: N0} milliseconds due to: {outcome.Exception?.Message ?? outcome.Result.ReasonPhrase}");
                    }
                );
        }
    }
}
