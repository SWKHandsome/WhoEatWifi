using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using WhoEatWifi.Core;
using WhoEatWifi.Models;
using WhoEatWifi.UI;

namespace WhoEatWifi
{
    public partial class MainWindow : Window
    {
        private readonly NetworkStatsManager _statsManager;
        private readonly TrayManager _trayManager;
        private ICollectionView _processesView;
        private string _searchKeyword = string.Empty;
        private int _sortMode = 0; // 0 = UsagePercentage, 1 = DownloadSpeed, 2 = DisplayName
        private bool _isSearchPlaceholder = true;
        private bool _isExplicitExit = false;
        private bool _isUpdatingLanguage = false;

        public MainWindow()
        {
            InitializeComponent();

            _statsManager = new NetworkStatsManager();
            _statsManager.HistoryManager.SessionSwitched += OnSessionSwitched;
            _statsManager.BudgetExceeded += OnBudgetExceeded;

            _trayManager = new TrayManager();
            _trayManager.RestoreRequested += delegate { RestoreWindow(); };
            _trayManager.ToggleMeteredRequested += delegate { OnToggleMeteredClick(null, null); };
            _trayManager.ResetSessionRequested += delegate { OnResetSessionClick(null, null); };
            _trayManager.ExitRequested += delegate { ExitApplication(); };

            // Bind process collection
            _processesView = CollectionViewSource.GetDefaultView(_statsManager.Processes);
            _processesView.Filter = ProcessFilter;
            ApplySorting();
            gridProcesses.ItemsSource = _processesView;

            // Bind history sessions
            lstHistorySessions.ItemsSource = _statsManager.HistoryManager.HistorySessions;

            // Initialize Language ComboBox & text safely after all elements are loaded
            if (cboLanguage != null)
            {
                cboLanguage.SelectedIndex = LanguageManager.CurrentLanguage == AppLanguage.English ? 0 : 1;
                cboLanguage.SelectionChanged += OnLanguageChanged;
            }
            UpdateLanguageUi();

            // UI Refresh timer (every 1 second)
            var uiTimer = new System.Windows.Threading.DispatcherTimer();
            uiTimer.Interval = TimeSpan.FromSeconds(1);
            uiTimer.Tick += OnUiTick;
            uiTimer.Start();

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
        }

        private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isUpdatingLanguage || cboLanguage == null) return;
            AppLanguage selected = cboLanguage.SelectedIndex == 0 ? AppLanguage.English : AppLanguage.Chinese;
            if (LanguageManager.CurrentLanguage != selected)
            {
                LanguageManager.CurrentLanguage = selected;
                UpdateLanguageUi();
            }
        }

        private void UpdateLanguageUi()
        {
            if (txtBadgeTitle == null || chkAlwaysOnTop == null || tabRealtime == null) return;

            this.Title = LanguageManager.T("AppTitle");
            txtBadgeTitle.Text = LanguageManager.T("BadgeTitle");
            txtAppSubtitle.Text = LanguageManager.T("AppSubtitle");
            chkAlwaysOnTop.Content = LanguageManager.T("AlwaysOnTop");
            btnMinToTray.Content = LanguageManager.T("MinToTray");
            btnOpenWifiSettings.Content = LanguageManager.T("OpenWifiSettings");
            btnRefresh.Content = LanguageManager.T("Refresh");

            // Cards
            lblCardNetworkTitle.Text = LanguageManager.T("CardNetworkTitle");
            btnToggleMetered.Content = LanguageManager.T("ToggleMeteredBtn");
            lblCardSpeedTitle.Text = LanguageManager.T("CardSpeedTitle");
            lblCardSessionTitle.Text = LanguageManager.T("CardSessionTitle");
            btnResetSession.Content = LanguageManager.T("ResetBtn");
            lblCardBudgetTitle.Text = LanguageManager.T("CardBudgetTitle");
            lblBudgetLabel.Text = LanguageManager.T("BudgetLabel");
            cboItemNoLimit.Content = LanguageManager.T("BudgetNoLimit");
            lblBudgetTip.Text = LanguageManager.T("BudgetTip");

            // Tab 1
            tabRealtime.Header = LanguageManager.T("TabRealtime");
            lblBreakdownTitle.Text = LanguageManager.T("BreakdownTitle");
            lblBreakdownSubtitle.Text = LanguageManager.T("BreakdownSubtitle");
            lblSortLabel.Text = LanguageManager.T("SortLabel");
            cboSortItem0.Content = LanguageManager.T("SortByPercentage");
            cboSortItem1.Content = LanguageManager.T("SortBySpeed");
            cboSortItem2.Content = LanguageManager.T("SortByName");

            colApp.Header = LanguageManager.T("ColApp");
            colPercentage.Header = LanguageManager.T("ColPercentage");
            colDownSpeed.Header = LanguageManager.T("ColDownSpeed");
            colUpSpeed.Header = LanguageManager.T("ColUpSpeed");
            colTotalBytes.Header = LanguageManager.T("ColTotalBytes");
            colActions.Header = LanguageManager.T("ColActions");

            if (_isSearchPlaceholder)
            {
                txtSearchFilter.Text = LanguageManager.T("SearchPlaceholder");
            }

            // Tab 2
            tabHistory.Header = LanguageManager.T("TabHistory");
            lblHistoryTip.Text = LanguageManager.T("HistoryTip");
            lblHistoryListTitle.Text = LanguageManager.T("HistoryListTitle");
            colHistApp.Header = LanguageManager.T("ColHistoryApp");
            colHistPct.Header = LanguageManager.T("ColHistoryPct");
            colHistDown.Header = LanguageManager.T("ColHistoryDown");
            colHistUp.Header = LanguageManager.T("ColHistoryUp");
            colHistTotal.Header = LanguageManager.T("ColHistoryTotal");

            // Bottom
            lblStatusBrand.Text = LanguageManager.T("StatusBrand");

            // Refresh items & dashboard
            if (_statsManager != null)
            {
                foreach (var p in _statsManager.Processes)
                {
                    p.RefreshLanguage();
                }
                UpdateDashboard();
            }

            OnHistorySessionSelectionChanged(null, null);
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _statsManager.Start();
            UpdateDashboard();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if (_isExplicitExit)
            {
                if (_trayManager != null) _trayManager.Dispose();
                if (_statsManager != null) _statsManager.Dispose();
                return;
            }

            string savedAction = LanguageManager.GetCloseAction();
            if (string.Equals(savedAction, "minimize", StringComparison.OrdinalIgnoreCase))
            {
                e.Cancel = true;
                this.Hide();
                return;
            }
            if (string.Equals(savedAction, "exit", StringComparison.OrdinalIgnoreCase))
            {
                _isExplicitExit = true;
                if (_trayManager != null) _trayManager.Dispose();
                if (_statsManager != null) _statsManager.Dispose();
                Application.Current.Shutdown();
                return;
            }

            CloseOptionDialog dialog = new CloseOptionDialog();
            dialog.Owner = this;
            dialog.ShowDialog();

            if (dialog.SelectedAction == CloseActionChoice.Minimize)
            {
                if (dialog.RememberChoice)
                {
                    LanguageManager.SetCloseAction("minimize");
                }
                e.Cancel = true;
                this.Hide();
            }
            else if (dialog.SelectedAction == CloseActionChoice.Exit)
            {
                if (dialog.RememberChoice)
                {
                    LanguageManager.SetCloseAction("exit");
                }
                _isExplicitExit = true;
                if (_trayManager != null) _trayManager.Dispose();
                if (_statsManager != null) _statsManager.Dispose();
                Application.Current.Shutdown();
            }
            else
            {
                // Canceled
                e.Cancel = true;
            }
        }

        private void RestoreWindow()
        {
            Dispatcher.Invoke(new Action(delegate
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
                this.Focus();
            }));
        }

        private void ExitApplication()
        {
            Dispatcher.Invoke(new Action(delegate
            {
                _isExplicitExit = true;
                this.Close();
                Application.Current.Shutdown();
            }));
        }

        private void OnMinimizeToTrayClick(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }

        private void OnUiTick(object sender, EventArgs e)
        {
            UpdateDashboard();

            // Refresh sorting and filtering
            if (_processesView != null)
            {
                _processesView.Refresh();
            }
        }

        private void UpdateDashboard()
        {
            // 1. SSID & Network info
            WifiInfo wifi = _statsManager.CurrentWifi;
            if (wifi != null)
            {
                txtSsid.Text = !string.IsNullOrEmpty(wifi.Ssid) ? wifi.Ssid : LanguageManager.T("Disconnected");
                txtAdapter.Text = LanguageManager.T("AdapterLabel",
                    !string.IsNullOrEmpty(wifi.InterfaceName) ? wifi.InterfaceName : (LanguageManager.CurrentLanguage == AppLanguage.English ? "Unknown" : "未知"),
                    !string.IsNullOrEmpty(wifi.InterfaceDescription) ? wifi.InterfaceDescription : (LanguageManager.CurrentLanguage == AppLanguage.English ? "Unknown Device" : "未知设备"));

                if (wifi.IsMetered)
                {
                    txtMeteredStatus.Text = LanguageManager.T("MeteredOn");
                    badgeMetered.Background = new SolidColorBrush(Color.FromRgb(254, 243, 199)); // amber
                    txtMeteredStatus.Foreground = new SolidColorBrush(Color.FromRgb(180, 83, 9));
                }
                else
                {
                    txtMeteredStatus.Text = LanguageManager.T("MeteredOff");
                    badgeMetered.Background = new SolidColorBrush(Color.FromRgb(220, 252, 231)); // green
                    txtMeteredStatus.Foreground = new SolidColorBrush(Color.FromRgb(21, 128, 61));
                }
            }

            // 2. Real-time Speeds
            txtTotalDownloadSpeed.Text = _statsManager.FormattedTotalDownloadSpeed;
            txtTotalUploadSpeed.Text = _statsManager.FormattedTotalUploadSpeed;

            // 3. Current Session Data Usage
            NetworkSession current = _statsManager.HistoryManager.CurrentSession;
            if (current != null)
            {
                txtSessionTotalData.Text = current.FormattedTotal;
                txtSessionBreakdown.Text = LanguageManager.T("SessionBreakdown", current.FormattedDownloaded, current.FormattedUploaded);
                txtSessionDuration.Text = LanguageManager.T("SessionDuration", current.FormattedDuration);
            }

            // 4. Update Top Segmented Breakdown Bar
            UpdateStackedBreakdownBar();

            // 5. Status Bar
            txtProcessCount.Text = LanguageManager.T("StatusAppsCount", _statsManager.Processes.Count);
            txtEtwStatus.Text = _statsManager.EtwStatus;

            if (_statsManager.IsPrecisionMode)
            {
                txtEtwStatusDot.Text = "●";
                txtEtwStatusDot.Foreground = new SolidColorBrush(Color.FromRgb(16, 185, 129)); // green for precision EStats
            }
            else
            {
                txtEtwStatusDot.Text = "●";
                txtEtwStatusDot.Foreground = new SolidColorBrush(Color.FromRgb(37, 99, 235)); // blue for smart attribution
            }

            // 6. Update Tray Status & Tooltip
            if (_trayManager != null)
            {
                string topAppInfo = string.Empty;
                var segments = _statsManager.GetTopBreakdownSegments();
                if (segments.Count > 0)
                {
                    topAppInfo = string.Format("{0} {1}", segments[0].Name, segments[0].FormattedPercentage);
                }

                _trayManager.UpdateStatus(
                    wifi != null ? wifi.Ssid : LanguageManager.T("Disconnected"),
                    wifi != null && wifi.IsMetered,
                    _statsManager.FormattedTotalDownloadSpeed,
                    current != null ? current.FormattedTotal : "0 B",
                    topAppInfo);
            }
        }

        private void UpdateStackedBreakdownBar()
        {
            var segments = _statsManager.GetTopBreakdownSegments();
            gridStackedBar.ColumnDefinitions.Clear();
            gridStackedBar.Children.Clear();
            panelBreakdownLegend.Children.Clear();

            if (segments.Count == 0)
            {
                txtBreakdownSummary.Text = LanguageManager.T("BreakdownWaiting");
                return;
            }

            int activeCount = 0;
            foreach (var p in _statsManager.Processes)
            {
                if (p.TotalBytes > 0) activeCount++;
            }

            txtBreakdownSummary.Text = LanguageManager.T("BreakdownSummary",
                activeCount, segments[0].Name, segments[0].FormattedPercentage);

            for (int i = 0; i < segments.Count; i++)
            {
                var seg = segments[i];

                // Column definition
                gridStackedBar.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(Math.Max(0.5, seg.Percentage), GridUnitType.Star)
                });

                // Segment block
                Border block = new Border
                {
                    Background = seg.ColorBrush,
                    ToolTip = string.Format("{0}: {1} ({2})", seg.Name, seg.FormattedPercentage, seg.FormattedBytes)
                };
                Grid.SetColumn(block, i);
                gridStackedBar.Children.Add(block);

                // Legend chip
                string usedTip = LanguageManager.CurrentLanguage == AppLanguage.English ?
                    string.Format("{0}: Total data {1}", seg.Name, seg.FormattedBytes) :
                    string.Format("{0}: 共已消耗 {1}", seg.Name, seg.FormattedBytes);
                StackPanel chip = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 0, 14, 4),
                    ToolTip = usedTip
                };

                Border dot = new Border
                {
                    Width = 8,
                    Height = 8,
                    CornerRadius = new CornerRadius(4),
                    Background = seg.ColorBrush,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 6, 0)
                };

                TextBlock txtName = new TextBlock
                {
                    Text = seg.Name,
                    FontSize = 11,
                    FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush(Color.FromRgb(30, 41, 59)),
                    MaxWidth = 130,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    VerticalAlignment = VerticalAlignment.Center
                };

                TextBlock txtPct = new TextBlock
                {
                    Text = string.Format(" {0}", seg.FormattedPercentage),
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = seg.ColorBrush,
                    VerticalAlignment = VerticalAlignment.Center
                };

                chip.Children.Add(dot);
                chip.Children.Add(txtName);
                chip.Children.Add(txtPct);

                panelBreakdownLegend.Children.Add(chip);
            }
        }

        private void OnSessionSwitched(object sender, SessionSwitchedEventArgs e)
        {
            Dispatcher.Invoke(new Action(delegate
            {
                string msg = LanguageManager.T("AlertNetworkSwitched", e.OldSsid, e.NewSsid);
                txtBannerMessage.Text = msg;
                bannerNotification.Background = new SolidColorBrush(Color.FromRgb(219, 234, 254)); // blue
                bannerNotification.BorderBrush = new SolidColorBrush(Color.FromRgb(147, 197, 253));
                txtBannerMessage.Foreground = new SolidColorBrush(Color.FromRgb(30, 64, 175));
                bannerNotification.Visibility = Visibility.Visible;

                UpdateDashboard();

                if (_trayManager != null)
                {
                    _trayManager.ShowNotification(
                        LanguageManager.T("NotifyNetworkSwitchedTitle"),
                        LanguageManager.T("NotifyNetworkSwitchedMsg", e.NewSsid),
                        System.Windows.Forms.ToolTipIcon.Info);
                }
            }));
        }

        private void OnBudgetExceeded(object sender, long totalBytes)
        {
            Dispatcher.Invoke(new Action(delegate
            {
                string msg = LanguageManager.T("AlertBudgetExceeded", AppTrafficRecord.FormatBytes(totalBytes));
                txtBannerMessage.Text = msg;
                bannerNotification.Background = new SolidColorBrush(Color.FromRgb(254, 226, 226)); // red
                bannerNotification.BorderBrush = new SolidColorBrush(Color.FromRgb(252, 165, 165));
                txtBannerMessage.Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28));
                bannerNotification.Visibility = Visibility.Visible;

                if (_trayManager != null)
                {
                    _trayManager.ShowNotification(
                        LanguageManager.T("NotifyBudgetTitle"),
                        LanguageManager.T("NotifyBudgetMsg", AppTrafficRecord.FormatBytes(totalBytes)),
                        System.Windows.Forms.ToolTipIcon.Warning);
                }

                try
                {
                    SystemSounds.Exclamation.Play();
                }
                catch { }
            }));
        }

        private bool ProcessFilter(object item)
        {
            if (string.IsNullOrEmpty(_searchKeyword)) return true;

            ProcessTrafficInfo info = item as ProcessTrafficInfo;
            if (info == null) return false;

            if (info.ProcessName.IndexOf(_searchKeyword, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (info.DisplayName.IndexOf(_searchKeyword, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            if (info.ProcessId.ToString().IndexOf(_searchKeyword, StringComparison.OrdinalIgnoreCase) >= 0) return true;

            return false;
        }

        private void ApplySorting()
        {
            if (_processesView == null) return;

            _processesView.SortDescriptions.Clear();
            if (_sortMode == 0) // Usage percentage & total bytes
            {
                _processesView.SortDescriptions.Add(new SortDescription("UsagePercentage", ListSortDirection.Descending));
                _processesView.SortDescriptions.Add(new SortDescription("TotalBytes", ListSortDirection.Descending));
            }
            else if (_sortMode == 1) // Download speed
            {
                _processesView.SortDescriptions.Add(new SortDescription("DownloadSpeedBps", ListSortDirection.Descending));
            }
            else if (_sortMode == 2) // DisplayName
            {
                _processesView.SortDescriptions.Add(new SortDescription("DisplayName", ListSortDirection.Ascending));
            }
        }

        private void OnAlwaysOnTopChanged(object sender, RoutedEventArgs e)
        {
            this.Topmost = chkAlwaysOnTop.IsChecked == true;
        }

        private void OnOpenWifiSettingsClick(object sender, RoutedEventArgs e)
        {
            WifiHelper.OpenWifiSettings();
        }

        private void OnRefreshClick(object sender, RoutedEventArgs e)
        {
            UpdateDashboard();
            if (_processesView != null) _processesView.Refresh();
        }

        private void OnDismissBannerClick(object sender, RoutedEventArgs e)
        {
            bannerNotification.Visibility = Visibility.Collapsed;
        }

        private void OnToggleMeteredClick(object sender, RoutedEventArgs e)
        {
            WifiInfo wifi = _statsManager.CurrentWifi;
            if (wifi == null || string.IsNullOrEmpty(wifi.Ssid))
            {
                MessageBox.Show(LanguageManager.CurrentLanguage == AppLanguage.English ? "No active Wi-Fi connection detected." : "当前未检测到活跃的 Wi-Fi 连接。",
                    LanguageManager.CurrentLanguage == AppLanguage.English ? "Notice" : "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            bool targetState = !wifi.IsMetered;
            bool success = WifiHelper.ToggleMeteredConnection(wifi.Ssid, targetState);
            if (success)
            {
                wifi.IsMetered = targetState;
                UpdateDashboard();
                string statusText = targetState ? LanguageManager.T("MeteredUpdatedOn") : LanguageManager.T("MeteredUpdatedOff");
                MessageBox.Show(statusText, LanguageManager.T("MeteredUpdatedTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                // If netsh fails, guide user to Windows settings
                MessageBoxResult res = MessageBox.Show(LanguageManager.T("MeteredGuideMsg"),
                    LanguageManager.T("MeteredGuideTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (res == MessageBoxResult.Yes)
                {
                    WifiHelper.OpenWifiSettings();
                }
            }
        }

        private void OnResetSessionClick(object sender, RoutedEventArgs e)
        {
            MessageBoxResult res = MessageBox.Show(LanguageManager.T("ConfirmResetMsg"), LanguageManager.T("ConfirmResetTitle"), MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (res == MessageBoxResult.Yes)
            {
                _statsManager.ResetCurrentSession();
                UpdateDashboard();
            }
        }

        private void OnBudgetLimitChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboBudgetLimit == null) return;

            int index = cboBudgetLimit.SelectedIndex;
            long limit = 0;
            switch (index)
            {
                case 1: limit = 500L * 1024 * 1024; break;       // 500 MB
                case 2: limit = 1024L * 1024 * 1024; break;      // 1 GB
                case 3: limit = 2L * 1024 * 1024 * 1024; break;  // 2 GB
                case 4: limit = 5L * 1024 * 1024 * 1024; break;  // 5 GB
                default: limit = 0; break;
            }

            if (_statsManager != null)
            {
                _statsManager.BudgetLimitBytes = limit;
            }
        }

        private void OnSearchGotFocus(object sender, RoutedEventArgs e)
        {
            if (_isSearchPlaceholder)
            {
                txtSearchFilter.Text = string.Empty;
                txtSearchFilter.Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42));
                _isSearchPlaceholder = false;
            }
        }

        private void OnSearchLostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearchFilter.Text))
            {
                _isSearchPlaceholder = true;
                txtSearchFilter.Text = LanguageManager.T("SearchPlaceholder");
                txtSearchFilter.Foreground = new SolidColorBrush(Color.FromRgb(148, 163, 184));
                _searchKeyword = string.Empty;
                if (_processesView != null) _processesView.Refresh();
            }
        }

        private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSearchPlaceholder) return;
            _searchKeyword = txtSearchFilter.Text.Trim();
            if (_processesView != null)
            {
                _processesView.Refresh();
            }
        }

        private void OnSortOptionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cboSortOption == null) return;
            _sortMode = cboSortOption.SelectedIndex;
            ApplySorting();
        }

        private void OnKillProcessClick(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null || btn.Tag == null) return;

            int pid;
            if (int.TryParse(btn.Tag.ToString(), out pid))
            {
                try
                {
                    Process p = Process.GetProcessById(pid);
                    string procName = p.ProcessName;

                    MessageBoxResult res = MessageBox.Show(
                        LanguageManager.T("ConfirmKillMsg", procName, pid),
                        LanguageManager.T("ConfirmKillTitle"),
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (res == MessageBoxResult.Yes)
                    {
                        p.Kill();
                        MessageBox.Show(LanguageManager.T("KillSuccess", procName), LanguageManager.T("ConfirmKillTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(LanguageManager.T("KillFailed", ex.Message), LanguageManager.T("ConfirmKillTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void OnOpenFolderClick(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if (btn == null || btn.Tag == null) return;

            string path = btn.Tag.ToString();
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                try
                {
                    Process.Start("explorer.exe", string.Format("/select,\"{0}\"", path));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(LanguageManager.T("LocateError", ex.Message), LanguageManager.T("ConfirmKillTitle"), MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show(LanguageManager.T("LocateFailed"), LanguageManager.T("ConfirmKillTitle"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void OnHistorySessionSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            NetworkSession selected = lstHistorySessions.SelectedItem as NetworkSession;
            if (selected != null)
            {
                txtHistoryDetailTitle.Text = LanguageManager.T("HistoryDetailTitle", selected.Ssid);
                txtHistoryDetailSubtitle.Text = LanguageManager.T("HistoryDetailSubtitle",
                    selected.FormattedTimeRange, selected.FormattedDuration, selected.FormattedTotal, selected.FormattedDownloaded, selected.FormattedUploaded);

                gridHistoryApps.ItemsSource = selected.AppRecords;
            }
            else
            {
                txtHistoryDetailTitle.Text = LanguageManager.T("HistoryDetailPlaceholder");
                txtHistoryDetailSubtitle.Text = string.Empty;
                gridHistoryApps.ItemsSource = null;
            }
        }
    }
}
