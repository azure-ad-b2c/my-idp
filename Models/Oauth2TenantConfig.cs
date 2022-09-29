using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace custom_idp.Models
{
    public class Oauth2TenantConfig
    {
        public string TenantId { get; set; }
        public AuthorizationConfig Authorization { get; set; } = new AuthorizationConfig();
        public TokenConfig Token { get; set; } = new TokenConfig();
        public string ClientId { get; set; } = "1234";
        public string ClientSecret { get; set; } = "abcd";
        public UserInfoConfig UserInfo { get; set; } = new UserInfoConfig();
        public OpenIdConfiguration OpenIdConfiguration { get; set; } = new OpenIdConfiguration();
        public JWKs JWKs { get; set; } = new JWKs();

    }

    public class AuthorizationConfig
    {
        public bool ReturnStateParam { get; set; } = true;
    }

    public class TokenConfig
    {
        public TokenHttpMethods HttpMethods { get; set; } = new TokenHttpMethods();
        public TokenAuthenticationMethods AuthMethods { get; set; } = new TokenAuthenticationMethods();
        public bool CheckCredentials { get; set; } = false;
    }

    public class TokenHttpMethods
    {
        public bool GET { get; set; } = true;
        public bool POST { get; set; } = true;
    }


    public class TokenAuthenticationMethods
    {
        public bool client_secret_post { get; set; } = true;
        public bool client_secret_basic { get; set; } = true;
        public bool private_key_jwt { get; set; } = true;
    }
    public class UserInfoConfig
    {
        public string QueryStringAccessTokenName { get; set; } = "access_token";
        public BearerTokenTransmissionMethod BearerTokenTransmissionMethod { get; set; } = new BearerTokenTransmissionMethod();
    }

    public class BearerTokenTransmissionMethod
    {
        public bool QueryString { get; set; } = true;
        public bool AuthorizationHeader { get; set; } = true;
    }

    public class OpenIdConfiguration
    {
        public bool Enabled { get; set; } = true;
    }
    
    public class JWKs
    {
        public bool Enabled { get; set; } = true;
    }

}
