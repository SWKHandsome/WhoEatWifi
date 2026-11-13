using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace WhoEatWifi
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            try
            {
                string oldFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "WhoEatWifi.exe.old");
                if (File.Exists(oldFile))
                {
                    File.Delete(oldFile);
                }
            }
            catch { }

            base.OnStartup(e);
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            LogException("DispatcherUnhandledException", e.Exception);
            MessageBox.Show(
                string.Format("程序发生界面异常:\n\n{0}\n\n详细信息已写入 crash.log", e.Exception.Message),
                "WhoEatWifi 界面异常",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            LogException("CurrentDomain_UnhandledException", ex);
            string msg = ex != null ? ex.Message : (e.ExceptionObject != null ? e.ExceptionObject.ToString() : "未知异常");
            MessageBox.Show(
                string.Format("程序发生严重错误:\n\n{0}\n\n详细信息已写入 crash.log", msg),
                "WhoEatWifi 崩溃错误",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }

        private static void LogException(string source, Exception ex)
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
                string content = string.Format(
                    "========================================\n时间: {0}\n来源: {1}\n异常: {2}\n========================================\n\n",
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    source,
                    ex != null ? ex.ToString() : "null");
                File.AppendAllText(path, content);
            }
            catch
            {
                // Ignore log write failure
            }
        }
    }
}
