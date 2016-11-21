using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShadowsocksFreeServerFetcher
{
    abstract class ServerInfoFetcher
    {
        abstract protected IEnumerable<ServerInfo> FetchServers();
        public IEnumerable<ServerInfo> GetServers()
        {
            try
            {
                IEnumerable<ServerInfo> servers = FetchServers().Where(server => server != null && server.IsValid());
                if (servers == null) return new List<ServerInfo>();
                return servers;
            }
            catch (Exception)
            {
                return new List<ServerInfo>();
            }
        }
    }

    public class ServerInfoFetcherAttribute : Attribute
    {
        private string name;
        public ServerInfoFetcherAttribute(string name)
        {
            this.name = name;
        }

        public string Name { get { return name; } }
    }

}
