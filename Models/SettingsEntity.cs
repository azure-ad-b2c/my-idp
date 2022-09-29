using System.Text.Json;
using Azure;
using Azure.Data.Tables;

namespace custom_idp.Models
{
    public class SettingsEntity : ITableEntity
    {
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
        public string? InstrumentationKey { get; set; }
        public string? OAuth2Settings { get; set; }


        public Oauth2TenantConfig GetOAuth2Settings()
        {
            // Deserialize the input JSON
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            Oauth2TenantConfig oauth2TenantConfig;

            if (string.IsNullOrEmpty(this.OAuth2Settings))
            {
                // If empty, create a new one
                oauth2TenantConfig = new Oauth2TenantConfig();
            }
            else
            {
                try
                {
                    // Deserialize the string from the Cosmos table API
                    oauth2TenantConfig = JsonSerializer.Deserialize<Oauth2TenantConfig>(this.OAuth2Settings!, options)!;
                }
                catch (System.Exception)
                {
                    // If can't deserialize, create a new one
                    oauth2TenantConfig = new Oauth2TenantConfig();
                }
                
            }

            oauth2TenantConfig.TenantId = this.RowKey;

            return oauth2TenantConfig;
        }
    }
}