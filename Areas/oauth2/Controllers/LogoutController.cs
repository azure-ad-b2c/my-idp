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
using Azure.Data.Tables;

namespace custom_idp.oauth2.Controllers
{
    [Area("oauth2")]
    public class LogoutController : Controller
    {
        const string EVENT = "Logout";
        private TelemetryClient _telemetry;
        private readonly ILogger<LogoutController> _logger;
        private SettingsService _settingsService;

        public LogoutController(ILogger<LogoutController> logger, TelemetryClient telemetry, SettingsService settingsService)
        {
            this._logger = logger;
            _telemetry = telemetry;
            _settingsService = settingsService;
        }

        [HttpGet]
        public string Index(string tenantId)
        {
            // Get the tenant settings
            SettingsEntity settings = _settingsService.GetConfig(tenantId);

            Commons.LogRequestAsync(Request, _telemetry, settings, tenantId, EVENT);
            return "Logout place holder.";
        }
    }
}
