using Application;
using Infrastructure;
using Scalar.AspNetCore;
using WebApi;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.AddMutualTls(builder.Configuration); // Only the API Gateway can connect.

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
//builder.Services.AddOpenApi();
builder.Services.AddOpenApi(options => { options.AddDocumentTransformer<BearerSecuritySchemeTransformer>(); }); // This config is to create a Bearer security scheme in the OpenAPI document, so the ScalarAPI can read it and use it for authentication (the Transformer must be created).

builder.Services.AddIdentitySettings();
builder.Services.AddJwtAuthentication(builder.Services.GetTokenSettings(builder.Configuration));

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddRabbitMQServiceConfiguration(builder.Services.GetRabbitMqSettings(builder.Configuration));

builder.Services.RegisterInventoryServiceClient(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
}

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "Inventory Service";
    options.DarkMode = true;
    options.DefaultHttpClient = new(ScalarTarget.CSharp, ScalarClient.HttpClient);
    options.CustomCss = "";
    options.AddPreferredSecuritySchemes("Bearer")
    .AddHttpAuthentication("Bearer", auth =>
    {
        auth.Token = null;
    });
});

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();