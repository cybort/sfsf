using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace ShadowsocksFreeServerFetcher
{
    [ServerInfoFetcher("shadowsocks8.com")]
    class UpdateByShadowsocks8 : ServerInfoFetcher
    {

        override protected IEnumerable<ServerInfo> FetchServers()
        {
            return (from index in Enumerable.Range(1, 3).AsParallel()
             select ServerInfoParser.ReadFromImageUrl(
                 "http://www.shadowsocks8.com/images/server0" + index + ".png"
                 )).ToArray();
        }

    }

}
