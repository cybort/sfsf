using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace ShadowsocksFreeServerFetcher
{
    static class ServerInfoParser
    {
        static public ServerInfo ReadFromText(string text, Regex parser)
        {
            try
            {
                List<ServerInfo> servers = new List<ServerInfo>();
                return ReadFromTextMatchResult(parser.Match(text));
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static ServerInfo ReadFromTextMatchResult(Match match)
        {
            try
            {
                if (match == null) return null;
                return new ServerInfo
                {
                    Host = match.Groups["host"].Value,
                    Port = match.Groups["port"].Value,
                    Method = match.Groups["method"].Value,
                    Password = match.Groups["password"].Value,
                    Status = true
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        static public ServerInfo[] ReadFromTextMulti(string text, Regex parser)
        {
            try
            {
                List<ServerInfo> servers = new List<ServerInfo>();
                foreach (Match match in parser.Matches(text))
                {
                    servers.Add(ReadFromTextMatchResult(match));
                }
                return servers.ToArray();
            }
            catch (Exception)
            {
                return new ServerInfo[0];
            }
        }

        static public ServerInfo ReadFromImageUrl(string url)
        {
            try
            {
                byte[] bytes = (new WebClient()).DownloadData(url);
                Bitmap bmp = new Bitmap(Image.FromStream(new MemoryStream(bytes)));
                return ReadFromImage(bmp);
            }
            catch (Exception)
            {
                return null;
            }
        }

        static public ServerInfo ReadFromImage(Bitmap bmp)
        {
            try
            {
                string ssSchema = (new ZXing.QrCode.QRCodeReader()).decode(new ZXing.BinaryBitmap(new ZXing.Common.HybridBinarizer(new ZXing.BitmapLuminanceSource(bmp)))).Text;
                if (ssSchema == null || !ssSchema.StartsWith("ss://")) return null;
                return ReadFromSsSchemaText(ssSchema);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static ServerInfo ReadFromSsSchemaText(string ssSchema)
        {
            try
            {
                string info = Encoding.UTF8.GetString(Convert.FromBase64String(ssSchema.Substring(5))).Trim();
                string[] items = info.Split(new char[] { ':', '@' }, StringSplitOptions.RemoveEmptyEntries);
                return new ServerInfo()
                {
                    Method = items[0],
                    Password = items[1],
                    Host = items[2],
                    Port = items[3],
                    Status = true,
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

    }

}
