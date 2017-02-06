using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace ShadowsocksFreeServerFetcher
{
    [ServerInfoFetcher("ashadowsocks.com")]
    class UpdateByAshadowsocks : ServerInfoFetcher
    {
        private string FetchJsonData()
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.ashadowsocks.com/tutorial/get_ports");
            var data = Encoding.ASCII.GetBytes("test=1");
            request.Method = "POST";
            request.ContentType = "application/json; charset=utf-8"; // But why
            request.ContentLength = data.Length;
            request.Referer = "https://www.ashadowsocks.com/tutorial/trial_port";
            string fxVersion = Math.Max(10 + ((DateTime.UtcNow - new DateTime(2012, 1, 31)).Days / 294) * 7, 10).ToString();
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:" + fxVersion + ".0) Gecko/20100101 Firefox/" + fxVersion + ".0";
            request.Headers["X-Requested-With"] = "XMLHttpRequest";
            using (var stream = request.GetRequestStream())
            {
                stream.Write(data, 0, data.Length);
            }
            var response = (HttpWebResponse)request.GetResponse();
            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();
            return responseString;
        }

        override protected IEnumerable<ServerInfo> FetchServers()
        {

            JArray result = JArray.Parse(FetchJsonData());

            List<ServerInfo> servers = new List<ServerInfo>();
            foreach (JObject item in result.Value<JArray>(1).Values<JObject>())
            {
                JObject server = item.Value<JObject>("Port");
                servers.Add(new ServerInfo
                {
                    Host = server.Value<string>("sshost"),
                    Port = server.Value<string>("ssport"),
                    Method = server.Value<string>("ssencrypt"),
                    Password = server.Value<string>("sspass"),
                    Status = server.Value<string>("status") != "2",
                });
            }

            return servers;
        }

    }
}
