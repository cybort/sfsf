using HtmlAgilityPack;
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
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create("http://shadowsocks8.com/");
            HttpWebResponse httpWebResponse = (HttpWebResponse)httpWebRequest.GetResponse();
            Uri baseUri = httpWebResponse.ResponseUri;
            HtmlDocument webpageDocument = new HtmlWeb().Load(baseUri.ToString());
            HtmlNodeCollection nodes = webpageDocument.DocumentNode.SelectNodes("//*[@id=\"free\"]//img[contains(@src, \"server\")]");
            return (
                from node in nodes.AsParallel()
                select ServerInfoParser.ReadFromImageUrl(
                    new Uri(baseUri, node.Attributes["src"].Value.ToString()).ToString()
                )
            ).ToArray();
        }

    }

}
