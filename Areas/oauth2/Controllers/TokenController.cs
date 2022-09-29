using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using custom_idp.Models;
using Microsoft.IdentityModel.Tokens;
using System.Security.Cryptography.X509Certificates;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.ApplicationInsights;
using System.Text.Json;
using Azure.Data.Tables;

namespace custom_idp.oauth2.Controllers
{
    [Area("oauth2")]
    public class TokenController : Controller
    {
        const string EVENT = "Token";
        private static Lazy<X509SigningCredentials> SigningCredentials = null!;
        private readonly ILogger<TokenController> _logger;
        private TelemetryClient _telemetry;
        private SettingsService _settingsService;

        public TokenController(ILogger<TokenController> logger, TelemetryClient telemetry, SettingsService settingsService)
        {
            this._logger = logger;
            _telemetry = telemetry;
            _settingsService = settingsService;

            SigningCredentials = Commons.LoadCertificate();
        }

        [HttpGet]
        [ActionName("index")]
        public async Task<IActionResult> IndexAsyncGet(string tenantId, string code)
        {
            // Get the tenant settings
            SettingsEntity settings = _settingsService.GetConfig(tenantId);

            // Check if HTTP GET is allowed
            if (!settings.GetOAuth2Settings().Token.HttpMethods.GET)
            {
                await Commons.LogRequestAsync(Request, _telemetry, settings, tenantId, EVENT, JsonSerializer.Serialize(new { error = "GET request is not allowed." }));
                return BadRequest(new { error = "GET request is not allowed." });
            }

            return await IndexCommonAsync(tenantId, code);
        }

        [HttpPost]
        [ActionName("index")]
        public async Task<IActionResult> IndexAsyncPost(string tenantId, string code)
        {
            // Get the tenant settings
            SettingsEntity settings = _settingsService.GetConfig(tenantId);

            // Check if HTTP POST is allowed
            if (!settings.GetOAuth2Settings().Token.HttpMethods.POST)
            {
                Commons.LogError(Request, _telemetry, settings, tenantId, EVENT, JsonSerializer.Serialize(new { error = "POST request is not allowed." }));
                return BadRequest(new { error = "POST request is not allowed." });
            }

            return await IndexCommonAsync(tenantId, code);
        }

        private async Task<IActionResult> IndexCommonAsync(string tenantId, string code)
        {

            // Get the tenant settings
            SettingsEntity settings = _settingsService.GetConfig(tenantId);

            if (string.IsNullOrEmpty(code))
            {
                Commons.LogError(Request, _telemetry, settings, tenantId, EVENT, "Code parameter is missing.");
                return new BadRequestObjectResult(new { error = "Code parameter is missing." });
            }

            // Try to get teh client_id and client_secret
            string ClientId = string.Empty;
            string ClientSecret = string.Empty;

            if (Request.Method == "POST")
            {
                ClientId = this.Request.Form["client_id"];
                ClientSecret = this.Request.Form["client_secret"];
            }
            else
            {
                ClientId = this.Request.Query["client_id"];
                ClientSecret = this.Request.Query["client_secret"];
            }

            // Check if client_secret_post authentication method is used
            if (!string.IsNullOrEmpty(ClientId) && !string.IsNullOrEmpty(ClientSecret))
            {
                // Check if client_secret_post is allowed
                if (!settings.GetOAuth2Settings().Token.AuthMethods.client_secret_post)
                {
                    Commons.LogError(Request, _telemetry, settings, tenantId, EVENT, "client_secret_post authentication method is not allowed");
                    return new BadRequestObjectResult(new { error = "client_secret_post authentication method is not allowed" });
                }

                // Check credentials
                if (settings.GetOAuth2Settings().Token.CheckCredentials &&
                    (settings.GetOAuth2Settings().ClientId != ClientId || settings.GetOAuth2Settings().ClientSecret != ClientSecret))
                {
                    Commons.LogError(Request, _telemetry, settings, tenantId, EVENT, "Invalid client_secret_post credentials");
                    return new UnauthorizedObjectResult(new { error = "Invalid client_secret_post credentials" });
                }
            }

            try
            {
                var base64EncodedBytes = System.Convert.FromBase64String(code);
                string codeString = System.Text.Encoding.UTF8.GetString(base64EncodedBytes);

                string client_id = codeString.Split('|')[1];
                string email = codeString.Split('|')[2];
                string JWT = Commons.BuildJwtToken(TokenController.SigningCredentials.Value, this.Request, tenantId, client_id, AppSettings.UserDisplayName, email);

                DateTime not_before = DateTime.Now.AddSeconds(-30);
                long not_beforeUnixTime = ((DateTimeOffset)not_before).ToUnixTimeSeconds();

                DateTime expires_on = DateTime.Now.AddDays(7);
                long expires_onUnixTime = ((DateTimeOffset)expires_on).ToUnixTimeSeconds();

                var payload = new
                {
                    access_token = JWT,
                    id_token = JWT,
                    token_type = "bearer",
                    refresh_token = "2723ff54-a7c6-4f66-b34a-332fcb9980b8",
                    not_before = not_beforeUnixTime,
                    expires_in = 43199,
                    expires_on = expires_onUnixTime,
                    scope = "email openid",
                    tenantId = tenantId
                };

                await Commons.LogRequestAsync(Request, _telemetry, settings, tenantId, EVENT, JsonSerializer.Serialize(payload));

                return new OkObjectResult(payload);
            }
            catch (System.Exception ex)
            {
                Commons.LogError(Request, _telemetry, settings, tenantId, EVENT, ex.Message);

                return new BadRequestObjectResult(ex.Message + " Code: " + code);
            }
        }
    }
}
