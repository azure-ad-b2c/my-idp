using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using custom_idp.Models;
using Microsoft.IdentityModel.Tokens;
using Microsoft.ApplicationInsights;
using Azure.Data.Tables;

namespace custom_idp.oauth2.Controllers
{
    [Area("oauth2")]
    public class AuthorizeController : Controller
    {
        const string EVENT = "Authorization";
        Random rand = new Random();
        private static Lazy<X509SigningCredentials> SigningCredentials = null!;
        private readonly ILogger<AuthorizeController> _logger;
        private TelemetryClient _telemetry;
        private SettingsService _settingsService;

        public AuthorizeController(ILogger<AuthorizeController> logger, TelemetryClient telemetry, SettingsService settingsService)
        {
            _logger = logger;
            _telemetry = telemetry;
            _settingsService = settingsService;
            SigningCredentials = Commons.LoadCertificate();
        }

        [ActionName("index")]
        public IActionResult Index(string tenantId)
        {
            // Get the tenant settings
            SettingsEntity settings = _settingsService.GetConfig(tenantId);

            this.ViewData["TenantId"] = tenantId;

            ViewData["client_id"] = string.IsNullOrEmpty(this.Request.Query["client_id"].ToString()) ? "default" : this.Request.Query["client_id"].ToString();
            ViewData["scope"] = string.IsNullOrEmpty(this.Request.Query["scope"].ToString()) ? "read" : this.Request.Query["scope"].ToString();
            ViewData["redirect_uri"] = string.IsNullOrEmpty(this.Request.Query["redirect_uri"].ToString()) ? "https://jwt.ms/#" : this.Request.Query["redirect_uri"].ToString();
            ViewData["state"] = string.IsNullOrEmpty(this.Request.Query["state"].ToString()) ? "abcd" : this.Request.Query["state"].ToString();

            Commons.LogRequestAsync(Request, _telemetry, settings, tenantId, EVENT);
            return View();
        }

        [HttpPost]
        [ActionName("index")]
        public RedirectResult SignIn(string tenantId, string email, string password, string redirect_uri, string client_id, string state)
        {
            // Get the tenant settings
            SettingsEntity settings = _settingsService.GetConfig(tenantId);

            string code = $"{rand.Next(12345, 99999)}|{client_id}|{email}";
            var codeTextBytes = System.Text.Encoding.UTF8.GetBytes(code);
            string id_token = Commons.BuildJwtToken(AuthorizeController.SigningCredentials.Value, this.Request, tenantId, client_id, AppSettings.UserDisplayName, email);
            //id_token={id_token}&
            string URL = $"{redirect_uri}?code={System.Convert.ToBase64String(codeTextBytes)}";

            // Return the state parameter
            if (settings.GetOAuth2Settings().Authorization.ReturnStateParam)
            {
                URL += $"&state={state.Replace("=", "%3D")}";
            }

            if (client_id == "default")
                URL = URL + $"&id_token={id_token}";

            Commons.LogRequestAsync(Request, _telemetry, settings, tenantId, EVENT, URL);
            return Redirect(URL);

        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
