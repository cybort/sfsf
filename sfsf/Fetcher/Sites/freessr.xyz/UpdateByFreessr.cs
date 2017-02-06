using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ShadowsocksFreeServerFetcher
{
    [ServerInfoFetcher("freessr.xyz")]
    class UpdateByFreessr : ServerInfoFetcher
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
            HtmlDocument webpageDocument = new HtmlWeb().Load("http://freessr.xyz/");
            HtmlNode contentNode = webpageDocument.DocumentNode.SelectSingleNode("//div[@class=\"row\"]");
            return ServerInfoParser.ReadFromTextMulti(contentNode.InnerText, parser);
        }

    }
}
