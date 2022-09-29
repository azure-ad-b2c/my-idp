
using System.Text.Json.Serialization;

namespace custom_idp.Models
{
    public class OidcConfigurationModel
    {
        [JsonPropertyName("issuer")]
        public string Issuer { get; set; } = null!;


        [JsonPropertyName("authorization_endpoint")]
        public string AuthorizationEndpoint { get; set; } = null!;


        [JsonPropertyName("token_endpoint")]
        public string TokenEndpoint { get; set; } = null!;


        [JsonPropertyName("end_session_endpoint")]
        public string EndSessionEndpoint { get; set; } = null!;


        [JsonPropertyName("userinfo_endpoint")]
        public string UserInfoEndpoint { get; set; } = null!;


        [JsonPropertyName("jwks_uri")]
        public string JwksUri { get; set; } = null!;


        [JsonPropertyName("id_token_signing_alg_values_supported")]
        public ICollection<string> IdTokenSigningAlgValuesSupported { get; set; } = null!;

        [JsonPropertyName("response_modes_supported")]
        public ICollection<string> response_modes_supported { get; set; } = null!;

        [JsonPropertyName("response_types_supported")]
        public ICollection<string> response_types_supported { get; set; } = null!;

        [JsonPropertyName("scopes_supported")]
        public ICollection<string> scopes_supported { get; set; } = null!;

        [JsonPropertyName("token_endpoint_auth_methods_supported")]
        public ICollection<string> token_endpoint_auth_methods_supported { get; set; } = null!;

        [JsonPropertyName("claims_supported")]
        public ICollection<string> claims_supported { get; set; } = null!;

        [JsonPropertyName("subject_types_supported")]
        public ICollection<string> subject_types_supported { get; set; } = null!;

    }
}