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

namespace custom_idp.Controllers
{
    public class HomeController : Controller
    {
        const string EVENT = "Home";
        private readonly ILogger<HomeController> _logger;
        private TelemetryClient _telemetry;
        private SettingsService _settingsService;

        public HomeController(ILogger<HomeController> logger, TelemetryClient telemetry, SettingsService settingsService)
        {
            _logger = logger;
            _telemetry = telemetry;
            _settingsService = settingsService;
        }

        public IActionResult Index(string tenantId)
        {
            SettingsEntity entity = _settingsService.GetConfig(tenantId);

            this.ViewData["TenantId"] = tenantId;

            this.ViewData["Version"] = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            //Commons.LogRequestAsync(Request, _telemetry, tenantId, EVENT);
            return View();
        }

        public IActionResult Privacy(string tenantId)
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
