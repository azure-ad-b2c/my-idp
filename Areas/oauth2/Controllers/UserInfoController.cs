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
using Microsoft.Extensions.Primitives;
using Microsoft.ApplicationInsights;
using System.Text.Json;
using Azure.Data.Tables;

namespace custom_idp.oauth2.Controllers
{
    [Area("oauth2")]
    public class UserInfoController : Controller
    {
        const string EVENT = "Info";
        private readonly ILogger<UserInfoController> _logger;
        private TelemetryClient _telemetry;
        private SettingsService _settingsService;

        public UserInfoController(ILogger<UserInfoController> logger, TelemetryClient telemetry, SettingsService settingsService)
        {
            this._logger = logger;
            _telemetry = telemetry;
            _settingsService = settingsService;
        }

        [HttpGet]
        public IActionResult Index(string tenantId)
        {
            // Get the tenant settings
            SettingsEntity settings = _settingsService.GetConfig(tenantId);

            string BearerToken = string.Empty;
            StringValues authorizationHeader;

            // Try to get it from the query string param
            if (settings.GetOAuth2Settings().UserInfo.BearerTokenTransmissionMethod.QueryString)
            {
                BearerToken = this.Request.Query[settings.GetOAuth2Settings().UserInfo.QueryStringAccessTokenName];
            }

            // If not found try to get it from the authorization HTTP header
            if (string.IsNullOrEmpty(BearerToken)
                && settings.GetOAuth2Settings().UserInfo.BearerTokenTransmissionMethod.AuthorizationHeader
                && this.Request.Headers.TryGetValue("Authorization", out authorizationHeader))
            {
                BearerToken = this.Request.Headers["Authorization"].ToString().Split(" ")[1];
            }

            // Check if the bearer token is not found
            if (string.IsNullOrEmpty(BearerToken))
            {
                Commons.LogError(Request, _telemetry, settings, tenantId, EVENT, "Bearer token not found");
                return new BadRequestObjectResult(new { error = "Bearer token not found" });
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            SecurityToken token = tokenHandler.ReadJwtToken(BearerToken);
            List<System.Security.Claims.Claim> claims = ((JwtSecurityToken)token).Claims.ToList();

            Dictionary<string, string> payload = new Dictionary<string, string>();

            foreach (var item in claims)
            {
                payload.Add(item.Type, item.Value);
            }

            Commons.LogRequestAsync(Request, _telemetry, settings, tenantId, EVENT, JsonSerializer.Serialize(payload));

            return Ok(payload);
        }
    }
}
