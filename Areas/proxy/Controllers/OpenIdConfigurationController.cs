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
using System.Text;
using Azure.Data.Tables;

namespace custom_idp.proxy.Controllers
{
    [Area("proxy")]
    public class OpenIdConfigurationController : Controller
    {
        const string EVENT = "ProxyOpenIdConfig";
        private readonly ILogger<OpenIdConfigurationController> _logger;
        private TelemetryClient _telemetry;
        private SettingsService _settingsService;
        private static readonly HttpClient _httpClient = new HttpClient();

        public OpenIdConfigurationController(ILogger<OpenIdConfigurationController> logger, TelemetryClient telemetry, SettingsService settingsService)
        {
            this._logger = logger;
            _telemetry = telemetry;
            _settingsService = settingsService;
        }

        [HttpGet]
        [ActionName("invoke")]
        public async Task<IActionResult> IndexGetAsync(string tenantId, string id)
        {
            // Get the tenant settings
            SettingsEntity settings = _settingsService.GetConfig(tenantId);

            Commons.LogRequestAsync(Request, _telemetry, settings, tenantId, EVENT + "Start", null, JsonSerializer.Serialize(new { Action = "Start reverse proxy", URL = id })).Wait();

            // Check if HTTP GET is allowed
            if (string.IsNullOrEmpty(id))
            {
                await Commons.LogRequestAsync(Request, _telemetry, settings, tenantId, EVENT, JsonSerializer.Serialize(new { error = "Target token URL is not configured." }));
                return BadRequest(new { error = "Cannot find the target identity provider well known configuration endpoint." });
            }
            try
            {
                HttpResponseMessage response = await CallIdentityProviderAsync(tenantId, Uri.UnescapeDataString(id));

                // Read the input claims from the response body
                string body = await response.Content.ReadAsStringAsync();

                OidcConfigurationModel payload = JsonSerializer.Deserialize<OidcConfigurationModel>(body);

                //Replace the token and user info endpoint to the proxy
                payload.TokenEndpoint = Url.ActionLink("Invoke", "Token", new { Area = "proxy", tenantId = tenantId }) + "/" + HttpRequestHelper.Base64Encode(payload.TokenEndpoint);
                payload.UserInfoEndpoint = Url.ActionLink("Invoke", "UserInfo", new { Area = "proxy", tenantId = tenantId }) + "/" + HttpRequestHelper.Base64Encode(payload.UserInfoEndpoint);

                Commons.LogRequestAsync(Request, _telemetry, settings, tenantId, EVENT + "End", JsonSerializer.Serialize(payload)).Wait();

                return Ok(payload);
            }
            catch (System.Exception ex)
            {
                Commons.LogError(Request, _telemetry, settings, tenantId, EVENT + "Error", ex.Message);
                return BadRequest(new { error = ex.Message });
            }
        }

        private async Task<HttpResponseMessage> CallIdentityProviderAsync(string tenantId, string IdpEndpoint)
        {
            var targetRequestMessage = await HttpRequestHelper.CreateRequestHttpMessageAsync(this.Request, IdpEndpoint);

            var responseMessage = await _httpClient.SendAsync(targetRequestMessage, HttpCompletionOption.ResponseHeadersRead);

            // Here we ask the framework to dispose the response object a the end of the user request
            this.HttpContext.Response.RegisterForDispose(responseMessage);

            return responseMessage;
        }



    }
}
