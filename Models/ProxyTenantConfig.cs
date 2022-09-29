using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace custom_idp.Models
{
    public class ProxyTenantConfig
    {
        public ProxyTokenConfig Token { get; set; } = new ProxyTokenConfig();
        public ProxyUserInfoConfig UserInfo { get; set; } = new ProxyUserInfoConfig();

    }

    public class ProxyTokenConfig
    {
        public string Url { get; set; } = "";
    }

    public class ProxyUserInfoConfig
    {
        public string Url { get; set; } = "";
    }


}
