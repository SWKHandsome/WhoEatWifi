using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WhoEatWifi.Core
{
    public class ProcessMeta
    {
        public string ProcessName { get; set; }
        public string DisplayName { get; set; }
        public string ExecutablePath { get; set; }
        public ImageSource Icon { get; set; }
    }

    public static class ProcessResolver
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, StringBuilder lpExeName, ref int lpdwSize);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool CloseHandle(IntPtr hObject);

        private const uint PROCESS_QUERY_LIMITED_INFORMATION = 0x1000;

        private static readonly Dictionary<int, ProcessMeta> _metaCache = new Dictionary<int, ProcessMeta>();
        private static readonly Dictionary<string, ImageSource> _iconCache = new Dictionary<string, ImageSource>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _syncRoot = new object();

        public static ProcessMeta Resolve(int pid)
        {
            lock (_syncRoot)
            {
                ProcessMeta cached;
                if (_metaCache.TryGetValue(pid, out cached))
                {
                    return cached;
                }
            }

            ProcessMeta meta = new ProcessMeta();
            meta.ProcessName = string.Format("PID {0}", pid);
            meta.DisplayName = meta.ProcessName;
            meta.ExecutablePath = string.Empty;

            if (pid == 0)
            {
                meta.ProcessName = "System Idle Process";
                meta.DisplayName = "系统空闲进程";
                lock (_syncRoot) { _metaCache[pid] = meta; }
                return meta;
            }
            if (pid == 4)
            {
                meta.ProcessName = "System";
                meta.DisplayName = "Windows 内核系统";
                lock (_syncRoot) { _metaCache[pid] = meta; }
                return meta;
            }

            try
            {
                Process p = Process.GetProcessById(pid);
                meta.ProcessName = p.ProcessName;
                meta.DisplayName = p.ProcessName;

                string exePath = GetProcessPath(pid);
                if (!string.IsNullOrEmpty(exePath))
                {
                    meta.ExecutablePath = exePath;

                    try
                    {
                        if (File.Exists(exePath))
                        {
                            FileVersionInfo vi = FileVersionInfo.GetVersionInfo(exePath);
                            if (!string.IsNullOrEmpty(vi.FileDescription))
                            {
                                meta.DisplayName = string.Format("{0} ({1}.exe)", vi.FileDescription, p.ProcessName);
                            }
                            else
                            {
                                meta.DisplayName = p.ProcessName + ".exe";
                            }

                            meta.Icon = GetAppIcon(exePath);
                        }
                    }
                    catch
                    {
                        // Ignore file permission errors
                    }
                }
            }
            catch
            {
                // Process may have already exited
            }

            lock (_syncRoot)
            {
                _metaCache[pid] = meta;
            }

            return meta;
        }

        private static string GetProcessPath(int pid)
        {
            IntPtr hProc = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, false, pid);
            if (hProc == IntPtr.Zero)
            {
                return string.Empty;
            }

            try
            {
                StringBuilder sb = new StringBuilder(1024);
                int size = sb.Capacity;
                if (QueryFullProcessImageName(hProc, 0, sb, ref size))
                {
                    return sb.ToString();
                }
            }
            finally
            {
                CloseHandle(hProc);
            }

            return string.Empty;
        }

        public static ImageSource GetAppIcon(string exePath)
        {
            if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            {
                return null;
            }

            lock (_syncRoot)
            {
                ImageSource cached;
                if (_iconCache.TryGetValue(exePath, out cached))
                {
                    return cached;
                }
            }

            try
            {
                using (Icon sysIcon = Icon.ExtractAssociatedIcon(exePath))
                {
                    if (sysIcon != null)
                    {
                        BitmapSource bs = Imaging.CreateBitmapSourceFromHIcon(
                            sysIcon.Handle,
                            Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions());
                        bs.Freeze();

                        lock (_syncRoot)
                        {
                            _iconCache[exePath] = bs;
                        }
                        return bs;
                    }
                }
            }
            catch
            {
                // Ignore icon extraction errors
            }

            return null;
        }
    }
}
