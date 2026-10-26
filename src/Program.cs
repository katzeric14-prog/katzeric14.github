using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Vexor.Core;
using Vexor.Ui;

namespace Vexor
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            if (UpdateService.IsApplyCommand(args))
            {
                UpdateService.ApplyFromCommandLine(args);
                return;
            }

            AppPaths.EnsureDirectories();
            Theme.Initialize();

            Application.ThreadException += delegate(object sender, ThreadExceptionEventArgs e)
            {
                ReportCrash(e.Exception);
            };
            AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
            {
                ReportCrash(e.ExceptionObject as Exception);
            };

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                ReportCrash(ex);
            }
        }

        private static void ReportCrash(Exception ex)
        {
            string text = ex != null ? ex.ToString() : "unknown error";
            try
            {
                AppPaths.EnsureDirectories();
                File.AppendAllText(Path.Combine(AppPaths.LogsDir, "crash.log"),
                    "=== " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + Environment.NewLine + text +
                    Environment.NewLine + Environment.NewLine);
            }
            catch
            {
            }
            try
            {
                MessageBox.Show(text, "Vexor Launcher", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch
            {
            }
        }
    }
}
