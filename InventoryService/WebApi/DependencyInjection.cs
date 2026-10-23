using Application.Settings;
using AuthLibrary.Constants.Authentication;
using AuthLibrary.Permissions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.IdentityModel.Tokens;
using Polly;
using Polly.Extensions.Http;
using ResponseLibrary.Wrappers;
using System.Net;
using System.Net.Security;
using System.Reflection;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
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

        // mTLS: HTTPS connections must present the gateway's client certificate, issued by the project CA.
        // Calls that bypass the gateway fail the TLS handshake before reaching any controller.
        internal static IWebHostBuilder AddMutualTls(this IWebHostBuilder webHost, IConfiguration config)
        {
            var settings = config.GetSection("MutualTls");
            var ca = X509CertificateLoader.LoadCertificateFromFile(settings["CaCertificatePath"]);
            var serverCertificate = X509CertificateLoader.LoadPkcs12FromFile(settings["ServerCertificatePath"], password: null);
            var allowedClientName = settings["AllowedClientName"];

            return webHost.ConfigureKestrel(kestrel => kestrel.ConfigureHttpsDefaults(https =>
            {
                https.ServerCertificate = serverCertificate;
                https.ClientCertificateMode = ClientCertificateMode.RequireCertificate;
                https.ClientCertificateValidation = (certificate, _, _) =>
                    certificate.GetNameInfo(X509NameType.SimpleName, false) == allowedClientName
                    && IsIssuedByCa(certificate, ca, ClientAuthentication);
            }));
        }

        private const string ClientAuthentication = "1.3.6.1.5.5.7.3.2"; // EKU OIDs.
        private const string ServerAuthentication = "1.3.6.1.5.5.7.3.1";

        private static bool IsIssuedByCa(X509Certificate2 certificate, X509Certificate2 ca, string keyUsage)
        {
            using var chain = new X509Chain();
            chain.ChainPolicy.TrustMode = X509ChainTrustMode.CustomRootTrust; // Trust only the project CA, not the OS store.
            chain.ChainPolicy.CustomTrustStore.Add(ca);
            chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck; // Limitation: dev CA has no CRL, so issued certs cannot be revoked.
            chain.ChainPolicy.ApplicationPolicy.Add(new Oid(keyUsage));

            return chain.Build(certificate);
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
            var ca = X509CertificateLoader.LoadCertificateFromFile(config["MutualTls:CaCertificatePath"]);

            services.AddHttpClient("ProductServiceClient", client =>
            {
                client.BaseAddress = new Uri(inventoryServiceClientSettings.ApiGatewayUrl);
            })
            // The gateway's HTTPS certificate is issued by the project CA; accept no other.
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                SslOptions =
                {
                    RemoteCertificateValidationCallback = (_, certificate, _, errors) =>
                        certificate is X509Certificate2 serverCertificate
                        && !errors.HasFlag(SslPolicyErrors.RemoteCertificateNameMismatch)
                        && IsIssuedByCa(serverCertificate, ca, ServerAuthentication)
                }
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
