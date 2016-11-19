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
                return FetchServers().Where(server => server != null && server.IsValid());
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
