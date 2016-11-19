using ShadowsocksFreeServerFetcher.Properties;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace ShadowsocksFreeServerFetcher
{
    // Information of IP address is from lite.ip2location.com
    class CountryIpTable
    {
        private Dictionary<int, string> CountryTable;
        private Dictionary<string, string> CountryName;
        private uint[] IpFrom;
        private int[] CountryId;
        private static CountryIpTable CurrentInstance;

        private CountryIpTable()
        {
            LoadIpInfo();
            LoadCountryName();
        }

        private void LoadIpInfo()
        { 
            string[] lines = Resources.IpTable.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            uint last = 0;
            List<uint> ipFrom = new List<uint>();
            List<int> countryId = new List<int>();
            Dictionary<string, int> CountryToId = new Dictionary<string, int>();
            CountryTable = new Dictionary<int, string>();
            int countryCount = 0;
            foreach (string line in lines)
            {
                string[] items = line.Split(null);

                uint count = UInt32.Parse(items[0]) * 256;
                string country = items[1];
                if (!CountryToId.ContainsKey(country))
                {
                    CountryToId[country] = countryCount;
                    CountryTable[countryCount] = country;
                    countryCount++;
                }
                int id = CountryToId[country];

                ipFrom.Add(last);
                countryId.Add(id);

                last += count;
            }
            IpFrom = ipFrom.ToArray();
            CountryId = countryId.ToArray();
        }

        private void LoadCountryName()
        {
            CountryName = new Dictionary<string, string>();
            string[] lines = Resources.CountryName.Split(new string[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string[] items = line.Split(null);
                if (items.Length != 2) continue;

                CountryName[items[0]] = items[1];
            }

        }

        public string Lookup(IPAddress[] ips)
        {
            foreach (IPAddress ip in ips)
            {
                try
                {
                    byte[] ipbytes = ip.MapToIPv4().GetAddressBytes();
                    return Lookup((uint)(
                        (ipbytes[0] << 24) |
                        (ipbytes[1] << 16) |
                        (ipbytes[2] << 8) |
                        (ipbytes[3])
                    ));
                }
                catch (Exception)
                {

                }
            }
            return null;
        }

        public static CountryIpTable Instance()
        {
            if (CurrentInstance == null)
            {
                CurrentInstance = new CountryIpTable();
            }
            return CurrentInstance;
        }

        public string Lookup(uint ip)
        {
            int index = Array.BinarySearch<uint>(IpFrom, ip);
            if (index < 0) index = ~index - 1;
            string country = CountryTable[CountryId[index]];
            if (country == "-") return null;
            return country;
        }

        public string GetCountryName(string country)
        {
            if (!CountryName.Keys.Contains(country)) return '[' + country + ']';
            return CountryName[country];
        }

    }
}
