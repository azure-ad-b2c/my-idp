using System.Text.Json;
using Azure;
using Azure.Data.Tables;

namespace custom_idp.Models
{
    public class SettingsService
    {
        private TableClient _tableClient;
        private readonly string PARTITION_KEY = "IDP";

        public SettingsService(TableClient tableClient)
        {
            _tableClient = tableClient;
        }

        public SettingsEntity GetConfig(string tenantId)
        {
            SettingsEntity entity;

            try
            {
                entity = _tableClient.GetEntity<SettingsEntity>(PARTITION_KEY, tenantId).Value;
            }
            catch
            {
                entity = CreateWithDefaultValues(tenantId);
            }


            return entity;
        }

        private SettingsEntity CreateWithDefaultValues(string tenantId)
        {
            // If entity not found create an empty one
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            SettingsEntity entity = new SettingsEntity();
            entity.PartitionKey = PARTITION_KEY;
            entity.RowKey = tenantId;

            entity.OAuth2Settings = JsonSerializer.Serialize(new Oauth2TenantConfig(), options);

            return entity;
        }

        public SettingsEntity UpsertConfig(string tenantId, SettingsEntity settingsEntity)
        {
            settingsEntity.PartitionKey = PARTITION_KEY;
            settingsEntity.RowKey = tenantId;
            _tableClient.UpsertEntity(settingsEntity);

            return settingsEntity;
        }

        public SettingsEntity DeleteConfig(string tenantId)
        {
            _tableClient.DeleteEntity(PARTITION_KEY, tenantId);

            return CreateWithDefaultValues(tenantId);;
        }
    }
}