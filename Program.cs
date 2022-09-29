using Azure.Data.Tables;
using custom_idp.Models;
using Microsoft.ApplicationInsights.Extensibility.Implementation;
using Microsoft.AspNetCore.HttpLogging;
using Microsoft.Extensions.Logging.AzureAppServices;

var builder = WebApplication.CreateBuilder(args);

// Add the output of the logger to local files
builder.Logging.AddAzureWebAppDiagnostics();
builder.Services.Configure<AzureFileLoggerOptions>(options =>
{
    options.FileName = "azure-diagnostics-a1-";
    options.FileSizeLimit = 50 * 1024;
    options.RetainedFileCountLimit = 5;
});

// Add services to the container.
builder.Services.AddControllersWithViews();

// Enable Application Insights telemetry
builder.Services.AddApplicationInsightsTelemetry();
TelemetryDebugWriter.IsTracingDisabled = true;

// Azure Blob table (Cosmos table API)
var connectionString = builder.Configuration.GetConnectionString("CosmosTableApi");
builder.Services.AddSingleton<TableClient>(new TableClient(connectionString, "CustomIDP"));
builder.Services.AddSingleton<SettingsService>();

// HTTP Logging middleware that logs information about HTTP requests and HTTP responses. 
// Add the request body to the log
// builder.Services.AddHttpLogging(options =>
// {
//     options.LoggingFields = HttpLoggingFields.All;
//     options.RequestBodyLogLimit = 4096;
//     options.ResponseBodyLogLimit = 4096;
// });

var app = builder.Build();

// Important to have it as early as possible
app.UseHttpLogging();

app.Use((context, next) =>
        {
            context.Request.EnableBuffering();
            return next();
        });

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

//app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Default areas
app.MapControllerRoute(
    name: "areas",
    pattern: "{tenantId:length(5,20)=default}/{area:exists}/{controller=Home}/{action=Index}/{id?}");

// OAuth2 well known configuration and keys
app.MapControllerRoute(
    name: "oauth2-config",
    pattern: "{tenantId:length(5,20)=default}/{area:exists}/.well-known/openid-configuration",
    new { controller = "OpenIdConfiguration", action = "Index" });

app.MapControllerRoute(
    name: "oauth2-jwks",
    pattern: "{tenantId:length(5,20)=default}/{area:exists}/.well-known/keys",
    new { controller = "OpenIdKeys", action = "Index" });

// Default routing (mainly for the home page)
app.MapControllerRoute(
    name: "default",
    pattern: "{tenantId:length(5,20)=default}/{controller=Home}/{action=Index}/{id?}");

app.Run();
