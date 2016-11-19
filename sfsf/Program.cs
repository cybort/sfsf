using ShadowsocksFreeServerFetcher.Properties;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace ShadowsocksFreeServerFetcher
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {

            if (Environment.OSVersion.Version.Major >= 6)
            {
                NativeMethods.SetProcessDPIAware();
            }

            Mutex mutex = new Mutex(false, "{526c534f-763e-4172-9ea0-61c448e88347}");
            if (mutex.WaitOne(TimeSpan.FromSeconds(1.0), false))
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                try
                {
                    Application.Run((ApplicationContext)new ShadowsocksFetcherApplication());
                }
                finally
                {
                    mutex.ReleaseMutex();
                }
            }
        }

        internal static class NativeMethods
        {
            [DllImport("user32.dll")]
            internal static extern bool SetProcessDPIAware();
        }

    }


}
