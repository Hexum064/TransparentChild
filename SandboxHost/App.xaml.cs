using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;

namespace SandboxHost
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static List<IntPtr> _children = new List<IntPtr>();
        public static List<int> _childrenProc = new List<int>();


        private Dispatcher _dispatcher = null;


        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            _dispatcher = Dispatcher.CurrentDispatcher;

            base.OnStartup(e);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {


            MessageBox.Show("GEH");

            ProcessStartInfo startInfo = new ProcessStartInfo();

            startInfo.UseShellExecute = false;

#if DEBUG
            startInfo.FileName = Path.GetFullPath(@"..\..\..\SandboxHost\bin\Debug\SandboxHost.exe");
#else
            startInfo.FileName = Path.GetFullPath(@"..\..\..\SandboxHost\bin\Release\SandboxHost.exe");          
#endif




            _children.ForEach((c) => startInfo.Arguments += c.ToString() + " ");

            //MessageBox.Show("Attemting to move: " + startInfo.Arguments);

            Process.Start(startInfo);
        }

    }
}
