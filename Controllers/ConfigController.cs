using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using custom_idp.Models;
using Microsoft.ApplicationInsights;
using System.Reflection;
using Azure.Data.Tables;
using System.Text.Json;

namespace custom_idp.Controllers
{
    public class ConfigController : Controller
    {
        const string EVENT = "Config";
        private readonly ILogger<ConfigController> _logger;
        private TelemetryClient _telemetry;
        private SettingsService _settingsService;

        public ConfigController(ILogger<ConfigController> logger, TelemetryClient telemetry, SettingsService settingsService)
        {
            _logger = logger;
            _telemetry = telemetry;
            _settingsService = settingsService;
        }

        [HttpGet]
        public IActionResult Index(string tenantId)
        {
            // Get the tenant settings
            SettingsEntity settings = _settingsService.GetConfig(tenantId);

            this.ViewData["TenantId"] = tenantId;
            this.ViewBag.InstrumentationKey = settings.InstrumentationKey;

            // Serialize the input JSON
            var serializeOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };

            this.ViewBag.OAuth2Settings = JsonSerializer.Serialize(settings.GetOAuth2Settings(), serializeOptions);

            return View();
        }

        [HttpPost]
        public IActionResult Index(string tenantId, string InstrumentationKey, string OAuth2Settings)
        {

            // Get the tenant settings
            SettingsEntity settings = _settingsService.GetConfig(tenantId);

            // Disable default tenant (default) changes
            if (tenantId.ToLower() == "default")
            {
                return BadRequest("Error: default tenant settings cannot be modified");
            }

            // Change the tenant ID of the Oauth2 settings
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };


            Oauth2TenantConfig oauth2TenantConfig = JsonSerializer.Deserialize<Oauth2TenantConfig>(OAuth2Settings, options)!;
            oauth2TenantConfig.TenantId = tenantId;

            // Update the values
            settings.OAuth2Settings = JsonSerializer.Serialize(oauth2TenantConfig);
            settings.InstrumentationKey = InstrumentationKey;
            settings = _settingsService.UpsertConfig(tenantId, settings);

            this.ViewData["TenantId"] = tenantId;
            this.ViewBag.InstrumentationKey = InstrumentationKey;

            // Serialize the input JSON
            var serializeOptions = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            this.ViewBag.OAuth2Settings = JsonSerializer.Serialize(settings.GetOAuth2Settings(), serializeOptions);

            return View();
        }
    }
}
