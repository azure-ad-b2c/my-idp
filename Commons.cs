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
using Microsoft.AspNetCore.Http;
using Microsoft.ApplicationInsights;
using System.Text.Json;
using custom_idp.proxy.Controllers;

namespace custom_idp
{
    public class Commons
    {

        public static Lazy<X509SigningCredentials> LoadCertificate()
        {
            return new Lazy<X509SigningCredentials>(() =>
            {

                X509Store certStore = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                certStore.Open(OpenFlags.ReadOnly);
                X509Certificate2Collection certCollection = certStore.Certificates.Find(
                                            X509FindType.FindByThumbprint,
                                            AppSettings.SigningCertThumbprint,
                                            false);
                // Get the first cert with the thumb-print
                if (certCollection.Count > 0)
                {
                    return new X509SigningCredentials(certCollection[0]);
                }

                throw new Exception("Certificate not found");
            });
        }
        public static string BuildJwtToken(X509SigningCredentials SigningCredentials, HttpRequest request, string tenantId, string ClientId, string Name, string email)
        {
            string issuer = $"{request.Scheme}://{request.Host}{request.PathBase.Value}/{tenantId}";

            // Token issuance date and time
            DateTime time = DateTime.Now;

            // All parameters send to Azure AD B2C needs to be sent as claims
            IList<System.Security.Claims.Claim> claims = new List<System.Security.Claims.Claim>();
            claims.Add(new System.Security.Claims.Claim("sub", email, System.Security.Claims.ClaimValueTypes.String, issuer));
            claims.Add(new System.Security.Claims.Claim("iat", ((DateTimeOffset)time).ToUnixTimeSeconds().ToString(), System.Security.Claims.ClaimValueTypes.Integer, issuer));
            claims.Add(new System.Security.Claims.Claim("name", email, System.Security.Claims.ClaimValueTypes.String, issuer));
            claims.Add(new System.Security.Claims.Claim("given_name", Name.Split(' ')[0], System.Security.Claims.ClaimValueTypes.String, issuer));
            claims.Add(new System.Security.Claims.Claim("family_name", Name.Split(' ')[1], System.Security.Claims.ClaimValueTypes.String, issuer));
            claims.Add(new System.Security.Claims.Claim("email", email, System.Security.Claims.ClaimValueTypes.String, issuer));
            claims.Add(new System.Security.Claims.Claim("tenantId", tenantId, System.Security.Claims.ClaimValueTypes.String, issuer));

            // Create the token
            JwtSecurityToken token = new JwtSecurityToken(
                    issuer,
                    ClientId,
                    claims,
                    time,
                    time.AddHours(24),
                    SigningCredentials);

            // Get the representation of the signed token
            JwtSecurityTokenHandler jwtHandler = new JwtSecurityTokenHandler();

            return jwtHandler.WriteToken(token);
        }

        public static async Task LogRequestAsync(
            HttpRequest Request,
            TelemetryClient telemetry,
            SettingsEntity settings,
            string tenantId,
            string page,
            HttpResponseMessage Response = null,
            string? responseBody = null,
            string? additionalData = null)
        {
            if (string.IsNullOrEmpty(settings.InstrumentationKey))
            {
                return;
            }

            telemetry.InstrumentationKey = settings.InstrumentationKey;

            // Request page
            telemetry.TrackPageView($"{tenantId}_{page}");

            Dictionary<string, string> log = new Dictionary<string, string>();

            log.Add("RequestMethod", Request.Method);
            log.Add("RequestURL", $"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}");
            log.Add("TenantId", tenantId);

            // Get the target URL
            if (Request.RouteValues.ContainsKey("area") && Request.RouteValues["area"]!.ToString() == "proxy" &&
                Request.RouteValues.ContainsKey("id"))
            {
                Uri targetUri = HttpRequestHelper.GetTargetUrl(Request, Request.RouteValues["id"]!.ToString());
                log.Add("IdpEndpointUrl", targetUri.ToString());
            }

            // Request headers
            string headers = JsonSerializer.Serialize(Request.Headers);
            log.Add("RequestHeaders", headers);

            // Request body
            try
            {
                if (Request.Method == "POST" && Request.Body != null)
                {
                    string body = "";
                    using (StreamReader stream = new StreamReader(Request.Body))
                    {
                        body = await stream.ReadToEndAsync();
                        log.Add("RequestBody", body);
                    }
                }
            }
            catch (ObjectDisposedException e)
            {
                // The body object has been disposed in earlier call to this function
            }
            catch (System.Exception)
            {
                throw;
            }

            // Response body
            if (Response != null)
            {
                log.Add("ResponseStatusCode", Response.StatusCode.ToString());
            }

            // Response body
            if (!string.IsNullOrEmpty(responseBody))
            {
                log.Add("ResponseBody", responseBody);
            }

            if (!string.IsNullOrEmpty(additionalData))
            {
                log.Add("AdditionalData", additionalData);
            }

            telemetry.TrackEvent($"{tenantId}_{page}", log);
            telemetry.Flush();
        }
        public static void LogError(HttpRequest Request, TelemetryClient telemetry, SettingsEntity settings, string tenantId, string page, string error, HttpResponseMessage Response = null)
        {
            if (string.IsNullOrEmpty(settings.InstrumentationKey))
            {
                return;
            }

            telemetry.InstrumentationKey = settings.InstrumentationKey;

            Dictionary<string, string> log = new Dictionary<string, string>();
            log.Add("RequestMethod", Request.Method);
            log.Add("RequestURL", $"{Request.Scheme}://{Request.Host}{Request.Path}{Request.QueryString}");
            log.Add("Error", error);
            log.Add("TenantId", tenantId);

            // Response body
            if (Response != null)
            {
                log.Add("ResponseStatusCode", Response.StatusCode.ToString());
            }

            telemetry.TrackEvent($"{tenantId}_{page}", log);
            telemetry.Flush();
        }
    }
}
