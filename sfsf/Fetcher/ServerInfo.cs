using Newtonsoft.Json;
using System.Linq;
using System;
using System.Net;
using System.Drawing;
using System.Text;
using System.IO;

namespace ShadowsocksFreeServerFetcher
{
    class ServerInfo
    {

        [JsonProperty("server")]
        public string Host { get; set; }

        [JsonProperty("server_port")]
        public string Port { get; set; }

        [JsonProperty("password")]
        public string Password { get; set; }

        [JsonProperty("method")]
        public string Method {
            get
            {
                return _method;
            }
            set
            {
                _method = value.ToLower();
            }
        }

        [JsonIgnore]
        private string _method;

        [JsonProperty("remarks")]
        public string Remarks
        {
            get
            {
                string country = Country;
                if (country == null) return null;
                return CountryIpTable.Instance().GetCountryName(country);
            }
        }

        [JsonIgnore]
        public bool Status { get; set; }

        private string cachedConutry = null;
        public string Country
        {
            get
            {
                try
                {
                    if (cachedConutry != null)
                    {
                        if (cachedConutry == "") return null;
                        return cachedConutry;
                    }
                    IPAddress[] ips = Dns.GetHostAddresses(Host);
                    if (ips.Length == 0) return null;
                    string country = CountryIpTable.Instance().Lookup(ips);
                    cachedConutry = country ?? "";
                    return country;
                }
                catch (Exception)
                {
                    // DNS 查询失败
                    return null;
                }
            }
        }

        public ServerInfo()
        {
        }
        
        public virtual bool IsValid()
        {
            if ((Host ?? "") == "") return false;
            if ((Port ?? "") == "") return false;
            if ((Password ?? "") == "") return false;
            if (!(new string[] {
                "table",
                "rc4-md5",
                "salsa20",
                "chacha20",
                "aes-256-cfb",
                "aes-192-cfb",
                "aes-128-cfb",
                "rc4",
            }).Contains(Method)) return false;
            int result;
            int.TryParse(Port, out result);
            if (result <= 0 || result > (int)ushort.MaxValue) return false;
            if (Status == false) return false;
            if (Country == null) return false;
            return true;
        }

    }
}
