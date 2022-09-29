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
    public class EndpointsController : Controller
    {
        const string EVENT = "Config";
        private readonly ILogger<EndpointsController> _logger;
        private TelemetryClient _telemetry;
        private SettingsService _settingsService;

        public EndpointsController(ILogger<EndpointsController> logger, TelemetryClient telemetry, SettingsService settingsService)
        {
            _logger = logger;
            _telemetry = telemetry;
            _settingsService = settingsService;
        }

        [HttpGet]
        public IActionResult Index(string tenantId)
        {
            GetLinks(tenantId);
            return View();
        }

        [HttpPost]
        public IActionResult Index(string tenantId, string idpConfigurationEndpoint,
                string idpTokenEndpoint, string idpUserInfoEndpoint)
        {
            // Configuration endpoint
            if (!string.IsNullOrEmpty(idpConfigurationEndpoint))
            {
                var idpConfigurationEndpointBytes = System.Text.Encoding.UTF8.GetBytes(idpConfigurationEndpoint);
                string proxyConfigurationEndpoint = Url.ActionLink("Invoke", "OpenIdConfiguration", new { Area = "proxy" })!;
                this.ViewBag.ProxyConfigurationEndpoint = $"{proxyConfigurationEndpoint}/{Convert.ToBase64String(idpConfigurationEndpointBytes).Replace("/", "_")}";
            }

            // Token endpoint
            if (!string.IsNullOrEmpty(idpTokenEndpoint))
            {
                var idpTokenEndpointBytes = System.Text.Encoding.UTF8.GetBytes(idpTokenEndpoint);
                string proxyTokenEndpoint = Url.ActionLink("Invoke", "token", new { Area = "proxy" })!;
                this.ViewBag.ProxyTokenEndpoint = $"{proxyTokenEndpoint}/{Convert.ToBase64String(idpTokenEndpointBytes).Replace("/", "_")}";
            }

            // User info endpoint
            if (!string.IsNullOrEmpty(idpUserInfoEndpoint))
            {
                var idpUserInfoEndpointBytes = System.Text.Encoding.UTF8.GetBytes(idpUserInfoEndpoint);
                string proxyUserInfoEndpoint = Url.ActionLink("Invoke", "userinfo", new { Area = "proxy" })!;
                this.ViewBag.ProxyUserInfoEndpoint = $"{proxyUserInfoEndpoint}/{Convert.ToBase64String(idpUserInfoEndpointBytes).Replace("/", "_")}";
            }

            GetLinks(tenantId);

            return View();
        }

        private void GetLinks(string tenantId)
        {
            this.ViewBag.OAuth2ConfigurationEndpoint = Url.Link("oauth2-config", new { Area = "oauth2", tenantId = tenantId });
            this.ViewBag.OAuth2JwksEndpoint = Url.Link("oauth2-jwks", new { Area = "oauth2", tenantId = tenantId });
            this.ViewBag.OAuth2AuthorizationEndpoint = Url.ActionLink("Index", "authorize", new { Area = "oauth2" })!;
            this.ViewBag.OAuth2TokenEndpoint = Url.ActionLink("Index", "token", new { Area = "oauth2" })!;
            this.ViewBag.OAuth2UserInfoEndpoint = Url.ActionLink("Index", "userinfo", new { Area = "oauth2" })!;
            this.ViewBag.OAuth2EndSessionEndpoint = Url.ActionLink("Index", "logout", new { Area = "oauth2" })!;
        }
    }
}
