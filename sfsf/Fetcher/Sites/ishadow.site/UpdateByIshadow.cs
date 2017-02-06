using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ShadowsocksFreeServerFetcher
{
    [ServerInfoFetcher("ishadow.site")]
    class UpdateByIshadowSite : ServerInfoFetcher
    {

        static Regex parser = new Regex(
            @"(?=服务器地址[\s:：][^\S\n\r]*(?<host>.*)\s*)" +
            @"(?=.(?:(?!服务器地址)[\s\S])*?端口[\s:：][^\S\n\r]*(?<port>\d*)\s*)" +
            @"(?=.(?:(?!服务器地址)[\s\S])*?密码[\s:：][^\S\n\r]*(?<password>.*)\s*)" +
            @"(?=.(?:(?!服务器地址)[\s\S])*?加密方式[\s:：][^\S\n\r]*(?<method>.*)\s*)" +
        "");

        override protected IEnumerable<ServerInfo> FetchServers()
        {
            HtmlDocument webpageDocument = new HtmlWeb().Load("http://www.ishadow.site/");
            HtmlNode serverText = webpageDocument.GetElementbyId("free");
            return ServerInfoParser.ReadFromTextMulti(serverText.InnerText, parser);
        }

    }
}
