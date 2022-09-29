using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;

namespace custom_idp.Models
{
    public class JwksModel
    {
        public ICollection<JwksKeyModel> Keys { get; set; } = null!;
    }
    public class JwksKeyModel
    {
        [JsonPropertyName("kid")]
        public string Kid { get; set; } = null!;

        [JsonPropertyName("nbf")]
        public long Nbf { get; set; } = 0;

        [JsonPropertyName("use")]
        public string Use { get; set; } = null!;

        [JsonPropertyName("kty")]
        public string Kty { get; set; } = null!;

        [JsonPropertyName("alg")]
        public string Alg { get; set; } = null!;

        [JsonPropertyName("x5c")]
        public ICollection<string> X5C { get; set; } = null!;

        [JsonPropertyName("x5t")]
        public string X5T { get; set; } = null!;

        [JsonPropertyName("n")]
        public string N { get; set; } = null!;

        [JsonPropertyName("e")]
        public string E { get; set; } = null!;

        public static JwksKeyModel FromSigningCredentials(X509SigningCredentials signingCredentials)
        {
            X509Certificate2 certificate = signingCredentials.Certificate;

            // JWK cert data must be base64 (not base64url) encoded
            string certData = Convert.ToBase64String(certificate.Export(X509ContentType.Cert));

            // JWK thumbprints must be base64url encoded (no padding or special chars)
            string thumbprint = Base64UrlEncoder.Encode(certificate.GetCertHash());

            // JWK must have the modulus and exponent explicitly defined
            RSACng? rsa = certificate.GetRSAPublicKey() as RSACng;

            if (rsa == null)
            {
                throw new Exception("Certificate is not an RSA certificate.");
            }

            RSAParameters keyParams = rsa.ExportParameters(false);
            string keyModulus = Base64UrlEncoder.Encode(keyParams.Modulus);
            string keyExponent = Base64UrlEncoder.Encode(keyParams.Exponent);

            return new JwksKeyModel
            {
                Kid = signingCredentials.Kid,
                Kty = "RSA",
                Nbf = new DateTimeOffset(certificate.NotBefore).ToUnixTimeSeconds(),
                Use = "sig",
                Alg = signingCredentials.Algorithm,
                X5C = new[] { certData },
                X5T = thumbprint,
                N = keyModulus,
                E = keyExponent
            };
        }
    }
}