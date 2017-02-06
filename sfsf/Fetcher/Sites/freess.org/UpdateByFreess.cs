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

        static Regex parser = new Regex(
            @"(?=服务器地址[\s:：][^\S\r\n]*(?<host>.*)\s*)" +
            @"(?=.(?:(?!服务器地址)[\s\S])*?端口[\s:：][^\S\r\n]*(?<port>\d*)\s*)" +
            @"(?=.(?:(?!服务器地址)[\s\S])*?密码[\s:：][^\S\r\n]*(?<password>.*)\s*)" +
            @"(?=.(?:(?!服务器地址)[\s\S])*?加密方式[\s:：][^\S\r\n]*(?<method>.*)\s*)" +
            @"(?=.(?:(?!服务器地址)[\s\S])*?状态[\s:：][^\S\r\n]*正常\s*)" +
        "");

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
