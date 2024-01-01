using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Data.Tables;
using custom_idp.Models;
using Microsoft.ApplicationInsights;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace custom_idp.oauth2.Controllers
{
    [Area("oauth2")]
    public class OpenIdKeysController : Controller
    {
        const string EVENT = "Keys";
        private static Lazy<X509SigningCredentials> SigningCredentials = null!;
        private TelemetryClient _telemetry;
        private readonly ILogger<OpenIdKeysController> _logger;
        private SettingsService _settingsService;


        public OpenIdKeysController(ILogger<OpenIdKeysController> logger, TelemetryClient telemetry, SettingsService settingsService)
        {
            this._logger = logger;
            _telemetry = telemetry;
            _settingsService = settingsService;

            SigningCredentials = Commons.LoadCertificate();
        }

        public ActionResult Index(string tenantId)
        {
            // Get the tenant settings
            SettingsEntity settings = _settingsService.GetConfig(tenantId);

            // Check if service availble
            if (!settings.GetOAuth2Settings().JWKs.Enabled)
            {
                Commons.LogRequestAsync(Request, _telemetry, settings, tenantId, EVENT, null, JsonSerializer.Serialize(new { error = "Service unavailable" }));
                return BadRequest(new { error = "Service unavailable" });
            }

            Commons.LogRequestAsync(Request, _telemetry, settings, tenantId, EVENT);

            try
            {
                JwksModel payload = new JwksModel
                {
                    Keys = new[] { JwksKeyModel.FromSigningCredentials(OpenIdKeysController.SigningCredentials.Value) }
                };

                return Ok(payload);
            }
            catch (System.Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }


        }
    }
}