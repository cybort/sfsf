using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ShadowsocksFreeServerFetcher
{
    [ServerInfoFetcher("freess.org")]
    class UpdateByFreess : ServerInfoFetcher
    {

        override protected IEnumerable<ServerInfo> FetchServers()
        {
            string pageUrl = "http://freess.org/";
            HtmlDocument webpageDocument = new HtmlWeb().Load(pageUrl);
            HtmlNodeCollection contentNodes = webpageDocument.DocumentNode.SelectNodes("//section[@id=\"portfolio-preview\"]//a[substring(@href, string-length(@href) - 3) = \".png\"]");
            return from contentNode in contentNodes.AsParallel()
                   select ServerInfoParser.ReadFromImageUrl(
                       new Uri(new Uri(pageUrl), contentNode.Attributes["href"].Value.ToString()).ToString()
                   );
        }

    }
}
