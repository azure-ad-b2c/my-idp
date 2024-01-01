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
    public class TokenController : Controller
    {
        const string EVENT = "ProxyToken";
        private readonly ILogger<TokenController> _logger;
        private TelemetryClient _telemetry;
        private SettingsService _settingsService;
        private static readonly HttpClient _httpClient = new HttpClient();

        public TokenController(ILogger<TokenController> logger, TelemetryClient telemetry, SettingsService settingsService)
        {
            this._logger = logger;
            _telemetry = telemetry;
            _settingsService = settingsService;
        }

        [HttpGet]
        [HttpPost]
        [ActionName("invoke")]
        public async Task<IActionResult> IndexGetAsync(string tenantId, string id)
        {
            HttpResponseMessage response = null;

            // Get the tenant settings
            SettingsEntity settings = _settingsService.GetConfig(tenantId);

            Commons.LogRequestAsync(Request, _telemetry, settings, tenantId, EVENT + "Start", null, null, JsonSerializer.Serialize(new { Action = "Start reverse proxy", Base64URL = id, URL = HttpRequestHelper.GetTargetUrl(Request, id) })).Wait();

            // Check if the custom IDP tenant ID exists
            if (string.IsNullOrEmpty(id))
            {
                await Commons.LogRequestAsync(Request, _telemetry, settings, tenantId, EVENT, null, JsonSerializer.Serialize(new { error = "Target token URL is not configured." }));
                return BadRequest(new { error = "Cannot find the target identity provider token endpoint." });
            }

            try
            {
                var targetRequestMessage = await HttpRequestHelper.CreateRequestHttpMessageAsync(this.Request, id);
                response = await _httpClient.SendAsync(targetRequestMessage, HttpCompletionOption.ResponseContentRead);

                // Read the input claims from the response body
                string body = await response.Content.ReadAsStringAsync();

                Commons.LogRequestAsync(Request, _telemetry, settings, tenantId, EVENT + "End", response, body).Wait();

                // Here we ask the framework to dispose the response object a the end of the user request
                this.HttpContext.Response.RegisterForDispose(response);

                // Return the respons
                return new HttpResponseMessageResult(response);
            }
            catch (System.Exception ex)
            {
                Commons.LogError(Request, _telemetry, settings, tenantId, EVENT + "Error", ex.Message, response);
                return BadRequest(new { error = ex.Message });
            }

        }
    }
}
