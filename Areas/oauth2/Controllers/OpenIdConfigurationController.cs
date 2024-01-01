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
    public class OpenIdConfigurationController : Controller
    {
        const string EVENT = "Configuration";
        private static Lazy<X509SigningCredentials> SigningCredentials = null!;
        private TelemetryClient _telemetry;
        private readonly ILogger<OpenIdConfigurationController> _logger;
        private SettingsService _settingsService;

        public OpenIdConfigurationController(ILogger<OpenIdConfigurationController> logger, TelemetryClient telemetry, SettingsService settingsService)
        {
            this._logger = logger;
            _telemetry = telemetry;
            _settingsService = settingsService;

            SigningCredentials = Commons.LoadCertificate();
        }

        public IActionResult Index(string tenantId)
        {

            // Get the tenant settings
            SettingsEntity settings = _settingsService.GetConfig(tenantId);

            // Check if service availble
            if (!settings.GetOAuth2Settings().OpenIdConfiguration.Enabled)
            {
                Commons.LogRequestAsync(Request, _telemetry, settings, tenantId, EVENT, null, null, JsonSerializer.Serialize(new { error = "Service unavailable" }));
                return BadRequest(new { error = "Service unavailable" });
            }

            OidcConfigurationModel payload = new OidcConfigurationModel
            {
                // The issuer name is the application root path
                Issuer = $"{this.Request.Scheme}://{this.Request.Host}{this.Request.PathBase.Value}/{tenantId}",

                // Include the absolute URL to JWKs endpoint
                JwksUri = Url.Link("oauth2-jwks", new { tenantId = tenantId })!,

                // End points
                AuthorizationEndpoint = Url.ActionLink("Index", "authorize")!,
                TokenEndpoint = Url.ActionLink("Index", "token")!,
                UserInfoEndpoint = Url.ActionLink("Index", "userinfo")!,
                EndSessionEndpoint = Url.ActionLink("Index", "logout")!,

                // Other metadata
                response_modes_supported = new[] { "query", "fragment", "form_post" },
                response_types_supported = new[] { "code", "code id_token", "id_token", "id_token token" },
                scopes_supported = new[] { "openid", "profile", "offline_access" },
                token_endpoint_auth_methods_supported = new[] { "client_secret_post", "private_key_jwt", "client_secret_basic" },
                claims_supported = new[] { "sub", "name", "email", "nbf", "exp", "iss", "aud", "iat", "auth_time", "acr", "nonce" },
                subject_types_supported = new[] { "pairwise" },

                // Include the supported signing algorithms
                IdTokenSigningAlgValuesSupported = new[] { OpenIdConfigurationController.SigningCredentials.Value.Algorithm }
            };

            Commons.LogRequestAsync(Request, _telemetry, settings, tenantId, EVENT, null, JsonSerializer.Serialize(payload));

            return Ok(payload);
        }
    }
}