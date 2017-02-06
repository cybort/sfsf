using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ShadowsocksFreeServerFetcher
{
    [ServerInfoFetcher("freevpnss.cc")]
    class UpdateByFreevpnss : ServerInfoFetcher
    {

        static Regex parser = new Regex(
            @"(?=服务器地址[\s:：][^\S\r\n]*(?<host>.*)\s*)" +
            @"(?=.(?:(?!服务器地址)[\s\S])*?端口[\s:：][^\S\r\n]*(?<port>\d*)\s*)" +
            @"(?=.(?:(?!服务器地址)[\s\S])*?密码[\s:：][^\S\r\n]*(?<password>.*)\s*)" +
            @"(?=.(?:(?!服务器地址)[\s\S])*?加密方式[\s:：][^\S\r\n]*(?<method>.*)\s*)" +
        "");

        override protected IEnumerable<ServerInfo> FetchServers()
        {
            HtmlDocument webpageDocument = new HtmlWeb().Load("http://freevpnss.cc/");
            HtmlNode node = webpageDocument.DocumentNode.SelectSingleNode("//*[@id=\"shadowsocks\"]/following-sibling::div");
            foreach (HtmlNode n in node.SelectNodes("//span[@class=\"hidden\"]")) n.ParentNode.RemoveChild(n);
            return ServerInfoParser.ReadFromTextMulti(node.InnerText, parser);
        }

    }
}
