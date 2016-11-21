using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using ShadowsocksFreeServerFetcher.Properties;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Timers;
using System.Windows.Forms;
using static System.Windows.Forms.Menu;

namespace ShadowsocksFreeServerFetcher
{
    internal class ShadowsocksFetcherApplication : ApplicationContext
    {

        private System.Timers.Timer UpdaterTimer;

        private NotifyIcon TrayIcon;
        private MenuItem RunInStartupMenuItem;
        private MenuItem ShowNotifyMenuItem;
        private MenuItem UpdateSourceMenuItems;
        private MenuItem FilterByCountryMenuItems;

        private List<ServerInfoFetcher> ServerInfoFetcherCollection;

        public ShadowsocksFetcherApplication()
        {

            // 需要指定 ss 配置文件的地址才能工作
            ReadOrPromptShadowsocksFilename();

            TrayIcon = new NotifyIcon() {
                Icon = Resources.AppIcon,
                Text = Resources.ProgramTitle,
                ContextMenu = new ContextMenu(new MenuItem[] {
                    new MenuItem(Resources.SetOutputMenuItem, new EventHandler(SetOutputMenuItem)),
                    RunInStartupMenuItem = new MenuItem(Resources.RunInStartupMenuItem, new EventHandler(RunInStartupMenuItemHandler)),
                    ShowNotifyMenuItem = new MenuItem(Resources.ShowNotifyMenuItem, new EventHandler(ShowNotifyMenuItemHandler)),
                    new MenuItem("-"),
                    (FilterByCountryMenuItems = new MenuItem(Resources.FilterByCountry)),
                    (UpdateSourceMenuItems = new MenuItem(Resources.UpdateSource)),
                    new MenuItem("-"),
                    new MenuItem(Resources.RestartShadowSock, new EventHandler(RestartShadowsockHandler)),
                    new MenuItem(Resources.UpdateNowMenuItem, new EventHandler(UpdateNowMenuItem)),
                    new MenuItem(Resources.ExitMenuItem, new EventHandler(ExitMenuItem)),
                }),
                Visible = true
            };
            UpdateRunInStartMenuItem();
            UpdateShowNotifyMenuItem();
            TrayIcon.DoubleClick += new EventHandler(UpdateNowMenuItem);

            UpdateServerInfoFetcher();
            DoUpdate(false);

            UpdaterTimer = new System.Timers.Timer()
            {
                Interval = 60000.0
            };
            UpdaterTimer.Elapsed += new ElapsedEventHandler(UpdateNowTimer);
            UpdaterTimer.Start();
        }

        ~ShadowsocksFetcherApplication()
        {

            TrayIcon.Visible = false;
        }

        private void ToggleServerInfoFetcher(string name, Type fetcher, MenuItem menuitem)
        {
            bool enabled = (menuitem.Checked = !menuitem.Checked);

            StringCollection enabledServers = Settings.Default.EnabledServers;
            if (enabledServers == null) Settings.Default.EnabledServers = new StringCollection();

            if (enabled)
            {
                Settings.Default.EnabledServers.Add(name);
                ServerInfoFetcherCollection.Add((ServerInfoFetcher)Activator.CreateInstance(fetcher));
            }
            else
            {
                Settings.Default.EnabledServers.Remove(name);
                ServerInfoFetcherCollection.RemoveAll(x => x.GetType() == fetcher);
            }
        }


        private void ClearServerInfoFetcher()
        {
            foreach (MenuItem menuitem in UpdateSourceMenuItems.MenuItems)
            {
                menuitem.Checked = false;
            }
            ServerInfoFetcherCollection.Clear();
        }

        private void UpdateServerInfoFetcher()
        {
            ServerInfoFetcherCollection = new List<ServerInfoFetcher>();

            List<KeyValuePair<string, Type>> candidateFetcher = new List<KeyValuePair<string, Type>>();
            if (true) {
                IEnumerable<Type> infoFetcherTypes =
                    from assembly in AppDomain.CurrentDomain.GetAssemblies()
                    from type in assembly.GetTypes()
                    where typeof(ServerInfoFetcher).IsAssignableFrom(type)
                    select type;
                foreach (Type infoFetcherType in infoFetcherTypes) {
                    ServerInfoFetcherAttribute[] describer = (ServerInfoFetcherAttribute[])infoFetcherType.GetCustomAttributes(typeof(ServerInfoFetcherAttribute), true);
                    if (describer.Length != 1 || (describer[0].Name ?? "") == "") continue;
                    string name = describer[0].Name;
                    candidateFetcher.Add(new KeyValuePair<string, Type>(name, infoFetcherType));
                }
                candidateFetcher = candidateFetcher.OrderBy(x => x.Key).ToList();
                if (candidateFetcher.Count == 0)
                {
                    throw new ApplicationException();
                }
            }
            foreach (KeyValuePair<string, Type> candidate in candidateFetcher)
            {
                MenuItem menuitem = new MenuItem(candidate.Key);
                menuitem.Click += delegate
                {
                    // 按住 Shift 再选择服务器源表示只打开该服务器源
                    if ((Control.ModifierKeys & Keys.Shift) == Keys.Shift)
                    {
                        ClearServerInfoFetcher();
                    }
                    ToggleServerInfoFetcher(candidate.Key, candidate.Value, menuitem);
                    DoUpdate(true);
                };
                UpdateSourceMenuItems.MenuItems.Add(menuitem);

                StringCollection enabledServers = Settings.Default.EnabledServers;
                if (enabledServers == null || enabledServers.Contains(candidate.Key))
                {
                    menuitem.Checked = true;
                    ServerInfoFetcher serverInfoFetcher = (ServerInfoFetcher)Activator.CreateInstance(candidate.Value);
                    ServerInfoFetcherCollection.Add(serverInfoFetcher);
                }
            }
        }

        /// <summary>
        /// 获取输出的配置文件的路径
        /// </summary>
        /// <returns>输出文件的路径</returns>
        private string OutputFileName()
        {
            return Path.Combine(Path.GetDirectoryName(GetShadowsocksFilename()), "gui-config.json");
        }

        /// <summary>
        /// 更新配置文件内容
        /// </summary>
        /// <returns>成功写入配置文件的服务器的数量，0表示配置文件未修改</returns>
        private int UpdateFile()
        {
            if (ServerInfoFetcherCollection.Count() == 0)
            {
                throw new ApplicationException(Resources.NoFetcherEnabled);
            }
            // 获取服务器信息
            ServerInfo[] serverInfoCollection = (
                from fetcher in ServerInfoFetcherCollection.AsParallel()
                select fetcher.GetServers()
            ).Aggregate((x, y) => x.Concat(y)).ToArray<ServerInfo>();

            serverInfoCollection = FilterByCountryAndUpdateCountryList(serverInfoCollection);

            if (serverInfoCollection.Count() == 0)
            {
                throw new ApplicationException(Resources.NoValidServer);
            }

            string str = (string)null;

            // 读取现有配置文件
            JObject currentSettings;
            try
            {
                currentSettings = JObject.Parse(File.ReadAllText(OutputFileName()));
                str = ((object)currentSettings).ToString();
            }
            catch (Exception)
            {
                currentSettings = new JObject();
            }

            // 根据之前的服务器选择更新选择的服务器
            int newIndex = -1;
            try
            {
                int oldIndex = currentSettings["index"].ToObject<int>();
                if (oldIndex >= 0)
                {
                    ServerInfo oldServer = ((JArray)currentSettings["configs"])[oldIndex].ToObject<ServerInfo>();
                    newIndex = BestMatchServer(serverInfoCollection, oldServer);
                }
            }
            catch (Exception) {}

            // 更新配置文件
            currentSettings["configs"] = (JToken)JArray.FromObject((object)serverInfoCollection);
            currentSettings["index"] = (JToken)new JValue(newIndex);
            if (newIndex == -1 && (
                currentSettings["strategy"] == null ||
                currentSettings["strategy"].Type == JTokenType.Null))
            {
                currentSettings["strategy"] = "com.shadowsocks.strategy.ha";
            }

            // 检查要输出的配置文件是否和原来完全一致，如果完全一致那么就不要打扰
            string contents = ((object)currentSettings).ToString();
            if (contents == str) return 0;
            
            File.WriteAllText(OutputFileName(), contents);
            return serverInfoCollection.Length;
        }

        /// <summary>
        /// 按照国家或地区的名称筛选服务器，同时更新程序中按国家或地区筛选的菜单
        /// </summary>
        /// <param name="serverInfoCollection"></param>
        /// <returns></returns>
        private ServerInfo[] FilterByCountryAndUpdateCountryList(ServerInfo[] servers)
        {
            MenuItemCollection menuItems = FilterByCountryMenuItems.MenuItems;
            menuItems.Clear();
            IEnumerable<string> countries = servers.Select(server => server.Country);
            IEnumerable<string> candidates = countries.Concat(new[] { "", Settings.Default.ChosenServerCountry });
            CountryIpTable countryIpTable = CountryIpTable.Instance();
            foreach (string country in candidates.OrderBy(x => x).Distinct())
            {
                string countryName = country == "" ? Resources.AnyCountry : countryIpTable.GetCountryName(country);
                menuItems.Add(new MenuItem(countryName, delegate
                {
                    if (Settings.Default.ChosenServerCountry == country) return;
                    Settings.Default.ChosenServerCountry = country;
                    DoUpdate(true);
                })
                {
                    Checked = country == Settings.Default.ChosenServerCountry,
                    RadioCheck = true,
                });
            }

            if ((Settings.Default.ChosenServerCountry ?? "") == "") return servers;
            return servers.Where(server => server.Country == Settings.Default.ChosenServerCountry).ToArray();

        }

        /// <summary>
        /// 根据之前用户选择的服务器，自动匹配新的服务器列表中与之最接近的服务器。
        /// 如果目前列表中有某台服务器与之前域名端口均相同则选择这一台。
        /// 否则优先匹配域名，其次匹配服务器所在国家。
        /// </summary>
        /// <param name="serverInfoCollection"></param>
        /// <param name="oldServer"></param>
        /// <returns>最匹配的服务器在数组中的索引，或-1表示无任何匹配</returns>
        private int BestMatchServer(ServerInfo[] serverInfoCollection, ServerInfo oldServer)
        {
            var currentMatch = new { Index = -1, Parts = 0, Country = false };
            string[] oldDomainParts = oldServer.Host.Split('.').Reverse().ToArray();
            foreach (var current in serverInfoCollection.Select((server, index) => new { server, index }))
            {
                if (oldServer.Host == current.server.Host && oldServer.Port == current.server.Port) return current.index;
                int matchPartsCount = current.server.Host.Split('.').Reverse().TakeWhile((part, index) => oldDomainParts[index] == part).Count();
                // FIXME: 目前实现较为简单没有使用Public Suffix List等方式判断
                if (matchPartsCount < 2) continue;

                bool matchCountry = current.server.Country == oldServer.Country;

                bool thisOneBetter = false;
                if (matchPartsCount > currentMatch.Parts) thisOneBetter = true;
                if (matchPartsCount == currentMatch.Parts && matchCountry && !currentMatch.Country) thisOneBetter = true;
                if (thisOneBetter)
                {
                    currentMatch = new { Index = current.index, Parts = matchPartsCount, Country = false };
                }
            }
            return currentMatch.Index;
        }

        /// <summary>
        /// 重新启动 shadowsock 程序
        /// </summary>
        private void RestartShadowsock()
        {
            string shadowsockFileName = Settings.Default.ShadowsockFileName;
            foreach (Process process in Process.GetProcesses())
            {
                try
                {
                    if (!object.Equals((object)process.MainModule.FileName, (object)shadowsockFileName))
                        continue;
                }
                catch (Exception)
                {
                    continue;
                }
                process.Kill();
            }
            Process.Start(shadowsockFileName);
        }

        private void RestartShadowsockHandler(object sender, EventArgs args)
        {
            RestartShadowsock();
        }

        /// <summary>
        /// 更新配置信息，并重启SS（如果需要）
        /// </summary>
        /// <param name="userActive">这次请求是否是用户触发的（或者是自动触发的）</param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        private bool DoUpdate(bool userActive = false)
        {
            bool isShowNotify = userActive || Settings.Default.ShowNotify;
            try
            {
                int serverCount = UpdateFile();
                if (serverCount == 0)
                {
                    if (userActive)
                        TrayIcon.ShowBalloonTip(4000, Resources.NoNeedUpdate, Resources.NoNeedUpdateTitle, ToolTipIcon.Info);
                }
                else
                {
                    RestartShadowsock();
                    if (isShowNotify)
                        TrayIcon.ShowBalloonTip(4000, string.Format(Resources.UpdateSuccess, (object)serverCount), Resources.UpdateSuccessTitle, ToolTipIcon.Info);
                }
                SetLastUpdateDate();
                return true;
            }
            catch (Exception ex)
            {
                if (isShowNotify)
                    TrayIcon.ShowBalloonTip(4000, ex.Message, Resources.UpdateFail, ToolTipIcon.Warning);
                return false;
            }
        }

        private void UpdateNowMenuItem(object sender, EventArgs e)
        {
            DoUpdate(true);
        }

        private void UpdateNowTimer(object sender, ElapsedEventArgs e)
        {
            DateTime lastUpdate = Settings.Default.LastUpdate;
            DateTime utcNow = DateTime.UtcNow;
            bool shouldUpdate = false;
            if (lastUpdate.Date != utcNow.Date) shouldUpdate = true;
            else if (lastUpdate.Hour != utcNow.Hour) shouldUpdate = true;
            else if (utcNow.Minute < 5) shouldUpdate = true;
            else if (utcNow.Minute - lastUpdate.Minute > 5) shouldUpdate = true;
            else shouldUpdate = false;
            if (shouldUpdate)
            {
                DoUpdate(false);
            }
        }

        /// <summary>
        /// 保存最后更新时间
        /// </summary>
        private void SetLastUpdateDate()
        {
            Settings.Default.LastUpdate = DateTime.UtcNow;
            Settings.Default.Save();
            TrayIcon.Text = Resources.ProgramTitle + Environment.NewLine + string.Format(Resources.LastUpdate, (object)DateTime.Now);
        }

        /// <summary>
        /// 选择 shadowsocks 的可执行程序的位置
        /// </summary>
        /// <returns>是否成功设置，如果输入被取消则返回false</returns>
        private bool PromptShadowsocksFilename()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog()
            {
                Filter = Resources.ShadowsockFileName + "|*.exe",
                RestoreDirectory = true,
                FileName = Settings.Default.ShadowsockFileName
            };
            if (openFileDialog.ShowDialog() != DialogResult.OK)
                return false;
            Settings.Default.ShadowsockFileName = openFileDialog.FileName;
            Settings.Default.Save();
            return true;
        }

        private void SetOutputMenuItem(object sender, EventArgs e)
        {
            if (!PromptShadowsocksFilename())
                return;
            DoUpdate(false);
        }

        private string GetShadowsocksFilename()
        {
            if (Settings.Default.ShadowsockFileName == "")
                PromptShadowsocksFilename();
            return Settings.Default.ShadowsockFileName;
        }

        private string ReadOrPromptShadowsocksFilename()
        {
            while (true) {
                string filename = GetShadowsocksFilename();
                if ((filename ?? "") != "") return filename;
                DialogResult msgboxResult = MessageBox.Show(
                    Resources.FileNameNeeded,
                    Resources.FileNameNeededTitle,
                    MessageBoxButtons.RetryCancel
                );
                if (msgboxResult == DialogResult.Cancel)
                {
                    throw new ApplicationException();
                }
            }
        }

        private void ExitMenuItem(object sender, EventArgs args)
        {
            Exit();
        }

        private void Exit()
        {
            Application.Exit();
        }

        private bool RegisterInStartup(bool? runInStartup = null)
        {
            RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            bool? nullable = runInStartup;
            bool flag1 = true;
            if ((nullable.GetValueOrDefault() == flag1 ? (nullable.HasValue ? 1 : 0) : 0) != 0)
            {
                registryKey.SetValue(Application.ProductName, (object)Application.ExecutablePath);
                return true;
            }
            nullable = runInStartup;
            bool flag2 = false;
            if ((nullable.GetValueOrDefault() == flag2 ? (nullable.HasValue ? 1 : 0) : 0) != 0)
            {
                registryKey.DeleteValue(Application.ProductName);
                return false;
            }
            return registryKey.GetValue(Application.ProductName) != null;
        }

        private void RunInStartupMenuItemHandler(object sender, EventArgs e)
        {
            RegisterInStartup(!RegisterInStartup());
            UpdateRunInStartMenuItem();
        }

        private void UpdateRunInStartMenuItem()
        {
            RunInStartupMenuItem.Checked = RegisterInStartup(new bool?());
        }

        private void ShowNotifyMenuItemHandler(object sender, EventArgs e)
        {
            Settings.Default.ShowNotify = !Settings.Default.ShowNotify;
            Settings.Default.Save();
            UpdateShowNotifyMenuItem();
        }

        private void UpdateShowNotifyMenuItem()
        {
            ShowNotifyMenuItem.Checked = Settings.Default.ShowNotify;
        }

    }
}
