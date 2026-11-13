using System;
using System.Drawing;
using System.Windows.Forms;
using WhoEatWifi.Core;

namespace WhoEatWifi.UI
{
    public class TrayManager : IDisposable
    {
        private readonly NotifyIcon _notifyIcon;
        private readonly ContextMenuStrip _contextMenu;

        private readonly ToolStripMenuItem _itemShow;
        private readonly ToolStripMenuItem _itemSsid;
        private readonly ToolStripMenuItem _itemMetered;
        private readonly ToolStripMenuItem _itemReset;
        private readonly ToolStripMenuItem _itemExit;

        public event EventHandler RestoreRequested;
        public event EventHandler ToggleMeteredRequested;
        public event EventHandler ResetSessionRequested;
        public event EventHandler ExitRequested;

        public TrayManager()
        {
            _notifyIcon = new NotifyIcon();
            _contextMenu = new ContextMenuStrip();

            _itemShow = new ToolStripMenuItem();
            _itemShow.Font = new Font(_itemShow.Font, FontStyle.Bold);
            _itemShow.Click += delegate { if (RestoreRequested != null) RestoreRequested(this, EventArgs.Empty); };

            _itemSsid = new ToolStripMenuItem();
            _itemSsid.Enabled = false;

            _itemMetered = new ToolStripMenuItem();
            _itemMetered.Click += delegate { if (ToggleMeteredRequested != null) ToggleMeteredRequested(this, EventArgs.Empty); };

            _itemReset = new ToolStripMenuItem();
            _itemReset.Click += delegate { if (ResetSessionRequested != null) ResetSessionRequested(this, EventArgs.Empty); };

            _itemExit = new ToolStripMenuItem();
            _itemExit.Click += delegate { if (ExitRequested != null) ExitRequested(this, EventArgs.Empty); };

            _contextMenu.Items.Add(_itemShow);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(_itemSsid);
            _contextMenu.Items.Add(_itemMetered);
            _contextMenu.Items.Add(_itemReset);
            _contextMenu.Items.Add(new ToolStripSeparator());
            _contextMenu.Items.Add(_itemExit);

            _notifyIcon.ContextMenuStrip = _contextMenu;
            _notifyIcon.Icon = TrayIconHelper.CreateWifiIcon();
            _notifyIcon.Visible = true;

            _notifyIcon.DoubleClick += delegate { if (RestoreRequested != null) RestoreRequested(this, EventArgs.Empty); };
            _notifyIcon.BalloonTipClicked += delegate { if (RestoreRequested != null) RestoreRequested(this, EventArgs.Empty); };

            LanguageManager.LanguageChanged += delegate { UpdateLanguage(); };
            UpdateLanguage();
        }

        public void UpdateLanguage()
        {
            _itemShow.Text = LanguageManager.T("TrayMenuShow");
            _itemReset.Text = LanguageManager.T("TrayMenuReset");
            _itemExit.Text = LanguageManager.T("TrayMenuExit");
            _notifyIcon.Text = LanguageManager.T("TrayTitle");
        }

        public void UpdateStatus(string ssid, bool isMetered, string downSpeed, string totalData, string topAppInfo)
        {
            try
            {
                // Update context menu items
                string netName = !string.IsNullOrEmpty(ssid) ? ssid : LanguageManager.T("Disconnected");
                _itemSsid.Text = LanguageManager.T("TrayMenuNetwork", netName);
                _itemMetered.Text = isMetered ? LanguageManager.T("TrayMenuMeteredOff") : LanguageManager.T("TrayMenuMeteredOn");

                // Tooltip text (keep within 63 chars for maximum compatibility)
                string brand = LanguageManager.T("TrayTitle");
                bool isEn = LanguageManager.CurrentLanguage == AppLanguage.English;
                string netLabel = isEn ? "Net" : "网络";
                string usedLabel = isEn ? "Used" : "已用";
                string speedLabel = isEn ? "Speed" : "速度";

                string tip;
                if (!string.IsNullOrEmpty(topAppInfo))
                {
                    tip = string.Format("{0}\n{1}: {2} | ⬇{3}\n{4}: {5} ({6})",
                        brand, netLabel, Truncate(ssid, 10), downSpeed, usedLabel, totalData, Truncate(topAppInfo, 16));
                }
                else
                {
                    tip = string.Format("{0}\n{1}: {2}\n{3}: ⬇{4} | {5}: {6}",
                        brand, netLabel, Truncate(ssid, 12), speedLabel, downSpeed, usedLabel, totalData);
                }

                if (tip.Length > 63)
                {
                    tip = tip.Substring(0, 60) + "...";
                }

                _notifyIcon.Text = tip;
            }
            catch
            {
                // Ignore tooltip format errors
            }
        }

        public void ShowNotification(string title, string message, ToolTipIcon icon)
        {
            try
            {
                _notifyIcon.ShowBalloonTip(3500, title, message, icon);
            }
            catch
            {
                // Ignore balloon tip error
            }
        }

        private static string Truncate(string val, int maxLen)
        {
            if (string.IsNullOrEmpty(val)) return string.Empty;
            if (val.Length <= maxLen) return val;
            return val.Substring(0, maxLen - 1) + "…";
        }

        public void Dispose()
        {
            if (_notifyIcon != null)
            {
                _notifyIcon.Visible = false;
                _notifyIcon.Dispose();
            }
            if (_contextMenu != null)
            {
                _contextMenu.Dispose();
            }
        }
    }
}
