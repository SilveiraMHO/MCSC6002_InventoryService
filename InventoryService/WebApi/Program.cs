using Application;
using Infrastructure;
using Scalar.AspNetCore;
using WebApi;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.AddMutualTls(builder.Configuration); // Only the API Gateway can connect.

builder.Services.AddControllers();
builder.Services.AddOpenApi(options => { options.AddDocumentTransformer<BearerSecuritySchemeTransformer>(); }); // Bearer scheme for Scalar.

builder.Services.AddIdentitySettings();
builder.Services.AddJwtAuthentication(builder.Services.GetTokenSettings(builder.Configuration));

builder.Services.AddApplicationServices();
builder.Services.AddInfrastructureServices(builder.Configuration);
builder.Services.AddRabbitMQServiceConfiguration(builder.Services.GetRabbitMqSettings(builder.Configuration));

builder.Services.RegisterInventoryServiceClient(builder.Configuration);

var app = builder.Build();

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