# WhoEatWifi 📶 (谁吃了我的 Wi-Fi)

<p align="center">
  <strong>Real-time Per-App Network Traffic Monitor & Mobile Hotspot Data Guard for Windows</strong>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Platform-Windows%2010%20%7C%2011%20(x64)-0078D6.svg?style=for-the-badge&logo=windows" alt="Platform" />
  <img src="https://img.shields.io/badge/.NET%20Framework-4.8-512BD4.svg?style=for-the-badge&logo=dotnet" alt=".NET 4.8" />
  <img src="https://img.shields.io/badge/Binary%20Size-~94%20KB-22C55E.svg?style=for-the-badge" alt="Size" />
  <img src="https://img.shields.io/badge/Security-100%25%20Offline%20%7C%20Zero%20Telemetry-10B981.svg?style=for-the-badge" alt="Security" />
  <img src="https://img.shields.io/badge/Dependencies-Zero%20Third--Party-3B82F6.svg?style=for-the-badge" alt="Dependencies" />
  <img src="https://img.shields.io/badge/License-MIT-gray.svg?style=for-the-badge" alt="License" />
</p>

---

## 💡 Why WhoEatWifi?

Have you ever connected your Windows PC to your smartphone's mobile hotspot, only to find **several gigabytes of cellular data wiped out in minutes**?

Background tasks such as **Windows Update**, **Microsoft OneDrive sync**, game launchers, and auto-updaters secretly consume bandwidth without warning. Standard Windows tools only show momentary speeds ($KB/s$) and do not clearly reveal **cumulative data percentages** or let you **stop data bleeding immediately**.

**WhoEatWifi** gives you total control over every byte of Wi-Fi and mobile hotspot data:
* 📊 **Live Cumulative Usage %**: Instantly see which apps consumed 80%+ of your hotspot session.
* 🛡️ **One-Click Metered Mode**: Command Windows Update and OneDrive to pause all background sync immediately.
* 📥 **System Tray Silent Guard**: Closes to tray, shows hover previews, and sounds alarms when your quota is exceeded.
* 🕒 **1-Hour Rolling History**: Switching Wi-Fi networks automatically archives the previous hotspot's data and resets to 0.
* 🌐 **Instant Bilingual Toggle**: Seamless switching between **English (default)** and **简体中文**.
* 🔒 **Pure & Safe Native Single Executable**: ~94 KB, zero third-party DLLs, zero installation, no driver required, zero inbound ports, 100% offline.

---

## ✨ Key Features

### 1. 📊 Mobile-Style App Usage Breakdown (%)
- **Cumulative Percentage Metric**:
  $$\text{Usage \%} = \frac{\text{App Total Bytes}}{\text{Session Total Bytes}} \times 100\%$$
- **Top 5 Proportional Stacked Bar**: Color-coded horizontal breakdown at the top (e.g. `[ Chrome 72.5% | OneDrive 18.2% | Steam 5.1% | Other Apps 4.2% ]`), giving you an instant bird's-eye view.
- **Process List with Visual Progress Bars**: Clear color badges highlighting heavy consumers (> 50% red, > 20% blue).
- **Default Sort by Data Usage %**: The heaviest bandwidth hog is always on top.

### 2. 🛡️ Hotspot Data Saver & Metered Network Control
- **1-Click "Metered Connection" Toggle**: Toggles Windows Metered Mode via native `WlanApi` / `netsh`. This officially commands Windows Update, Store downloads, and OneDrive to stop all background downloads.
- **Hotspot Budget Exceeded Alert**: Choose a quota (500 MB / 1.0 GB / 2.0 GB / 5.0 GB). When breached, an audio exclamation sounds, a banner alerts you, and a system tray notification warns you.
- **One-Click "End Task" (止血结束进程)**: Terminate runaway bandwidth-hogging processes with a single click.
- **Open File Location**: Instantly reveal suspicious executables in Windows File Explorer.

### 3. 📥 Background System Tray Silent Monitoring
- **Minimize to Tray on [X]**: Closing the window quietly docks the app in the Windows taskbar tray (Notification Area), keeping your desktop uncluttered.
- **Hover Mini-Dashboard**: Hovering your mouse over the blue Wi-Fi tray icon displays:
  ```text
  WhoEatWifi Data Guard
  Net: iPhone_Hotspot | ⬇ 2.4 MB/s
  Used: 420.5 MB (Chrome 78.4%)
  ```
- **Context Menu Quick Controls**: Double-click to restore, or right-click to Show Dashboard, Toggle Metered Mode, Reset counter, or Exit.
- **Desktop Toast Warnings**: Notifies you even while the main window is hidden when switching networks or exceeding quotas.

### 4. 🕒 Wi-Fi Switch Sensing & 1-Hour Rolling History
- **Automatic Hotspot Sensing**: Detects Wi-Fi adapter reconnection or SSID handoffs (e.g., disconnecting from your home fiber Wi-Fi and tethering to your mobile hotspot).
- **Zero-Count Reset**: Automatically resets live session counters upon network handoff so current hotspot stats remain 100% accurate.
- **1-Hour Rolling Retention**: Archives prior sessions with their exact start/end time, total bandwidth, and full per-app consumption list. Sessions older than 60 minutes are automatically pruned to conserve memory.

### 5. 🌐 Instant Bilingual Support (English & 简体中文)
- **English Default**: Out-of-the-box international experience.
- **1-Click Header Switch**: Convenient dropdown in the top-right header `[ English / 简体中文 ▼ ]`.
- **Zero-Restart Live Refresh**: All titles, dashboard cards, grid headers, action buttons, tray menus, and system alerts update instantaneously.
- **Persistent Preferences**: Saves your choice to `settings.ini` alongside the executable.

### 6. 🔒 100% Safe, Offline & Privacy-Compliant
- **No Inbound Listening Ports**: Does not open any socket listeners, web servers, or proxy servers.
- **No Outbound Telemetry**: Zero analytics, tracking, or network requests sent.
- **Standard User Privilege (`asInvoker`)**: Launches instantly without nagging UAC administrator prompts.
- **Engineered for Zero Crashes**: Uses stable Windows IP Helper APIs (`GetExtendedTcpTable`, `GetExtendedUdpTable`, `GetPerTcpConnectionEStats`) and `System.Net.NetworkInformation.NetworkInterface` with comprehensive crash logging (`crash.log`).

---

## 🖥️ User Interface Overview

| Dashboard Section | Description |
| :--- | :--- |
| **Connected Network** | Real-time SSID, network adapter name, signal, and Metered Mode badge |
| **Real-time Speeds** | Big typography indicators for live download (⬇️ MB/s) and upload (⬆️ KB/s) |
| **Current Session Usage** | Total session data with download/upload split, duration timer, and Reset button |
| **Hotspot Budget Alert** | Configurable quota limit dropdown with automatic warning triggers |
| **Traffic Breakdown Bar** | Proportional mobile-style stacked bar visualizing Top 5 consumers |
| **Processes DataGrid** | Per-process icon, PID, cumulative %, progress bar, live speed, and quick actions |
| **1-Hour History Tab** | Master-detail browser inspecting previous Wi-Fi sessions and app consumption |
| **System Tray** | Blue Wi-Fi icon, hover tooltip with top consumer %, and quick action menu |

---

## 🚀 Quick Start

### 1. Run Pre-Compiled Binary (No Installation)
A ready-to-use standalone executable is compiled directly in `bin\Release\`:
> **Double-click [`bin\Release\WhoEatWifi.exe`](bin/Release/WhoEatWifi.exe)** to start guarding your Wi-Fi immediately!

*(No installer, no third-party runtimes, no dependencies. Simply copy `WhoEatWifi.exe` to a USB drive or any folder).*

### 2. Build From Source
WhoEatWifi can be built with the standard MSBuild engine included with every modern Windows installation:

```powershell
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" "WhoEatWifi.csproj" /p:Configuration=Release /p:Platform=x64 /t:Rebuild
```

### 3. Run Automated Unit Tests
To verify all core statistics, percentage calculations, network switch detection, 1-hour pruning, and bilingual dictionaries:

```powershell
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:exe /out:bin\TestRunner.exe Tests\CoreTests.cs Core\SessionHistoryManager.cs Core\ProcessResolver.cs Core\LanguageManager.cs Models\AppTrafficRecord.cs Models\NetworkSession.cs Models\ProcessTrafficInfo.cs Models\TrafficSegment.cs /reference:C:\WINDOWS\Microsoft.Net\assembly\GAC_64\PresentationCore\v4.0_4.0.0.0__31bf3856ad364e35\PresentationCore.dll /reference:C:\WINDOWS\Microsoft.Net\assembly\GAC_MSIL\WindowsBase\v4.0_4.0.0.0__31bf3856ad364e35\WindowsBase.dll
& "bin\TestRunner.exe"
```
*(All 25 automated unit tests pass with 0 failures).*

---

## 🏛️ Project Architecture

```
d:\Github\WhoEatWifi\
├── App.xaml                    # WPF modern dark/light typography & control templates
├── App.xaml.cs                 # Global crash-safe exception dispatchers & logging
├── App.manifest                # asInvoker execution privileges (no UAC harassment)
├── MainWindow.xaml             # Modern dual-tab WPF dashboard with responsive layout
├── MainWindow.xaml.cs          # UI event routing, live language switcher & data-binding
├── WhoEatWifi.csproj           # Standalone single-assembly MSBuild project
├── Core/
│   ├── IphlpNetworkMonitor.cs  # High-performance TCP/UDP socket & EStats polling
│   ├── NetworkStatsManager.cs  # 1-second statistics engine, quota monitor & top aggregator
│   ├── WifiHelper.cs           # Native WlanApi SSID detection & netsh metered toggle
│   ├── ProcessResolver.cs      # PID resolution, product name caching & WPF icon extractor
│   ├── SessionHistoryManager.cs# Network switch detector & 1-hour rolling session history
│   └── LanguageManager.cs      # Full bilingual translation dictionary & settings.ini store
├── Models/
│   ├── ProcessTrafficInfo.cs   # INotifyPropertyChanged active app network telemetry
│   ├── NetworkSession.cs       # Archived session with duration & byte aggregations
│   ├── AppTrafficRecord.cs     # Historical app consumption snapshot record
│   └── TrafficSegment.cs       # Data model for the top horizontal stacked bar
├── UI/
│   ├── TrayManager.cs          # Windows Forms NotifyIcon, context menu & hover tooltip
│   ├── TrayIconHelper.cs       # Dynamic GDI+ blue rounded Wi-Fi icon drawing
│   └── ValueConverters.cs      # WPF IValueConverters for speeds, bytes & brush gradients
├── Tests/
│   └── CoreTests.cs            # 25-suite automated unit verification harness
└── bin/Release/
    └── WhoEatWifi.exe          # Production standalone single executable (~94 KB)
```

---

## 📄 License

This project is licensed under the [MIT License](LICENSE).
