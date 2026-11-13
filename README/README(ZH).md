# WhoEatWifi 📶 (谁吃了我的 Wi-Fi)

<p align="center">
  <strong>专为 Windows 打造的轻量级、高精度 Wi-Fi 与手机热点实时流量监控与防偷跑卫士</strong>
</p>

<p align="center">
  📖 <a href="../README.md">English Documentation</a> | <a href="README(ZH).md">简体中文文档</a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/运行平台-Windows%2010%20%7C%2011%20(x64)-0078D6.svg?style=for-the-badge&logo=windows" alt="Platform" />
  <img src="https://img.shields.io/badge/目标框架-.NET%20Framework%204.8-512BD4.svg?style=for-the-badge&logo=dotnet" alt=".NET 4.8" />
  <img src="https://img.shields.io/badge/程序体积-~94%20KB-22C55E.svg?style=for-the-badge" alt="Size" />
  <img src="https://img.shields.io/badge/安全隐私-100%25%20纯离线%20%7C%20零数据收集-10B981.svg?style=for-the-badge" alt="Security" />
  <img src="https://img.shields.io/badge/依赖组件-零第三方依赖-3B82F6.svg?style=for-the-badge" alt="Dependencies" />
  <img src="https://img.shields.io/badge/开源协议-MIT-gray.svg?style=for-the-badge" alt="License" />
</p>

---

## 💡 为什么需要 WhoEatWifi？

当您将 Windows 电脑连接到手机的**移动热点 (Personal Hotspot)** 时，您是否经历过**短短几分钟内数个 G 的流量套餐被洗劫一空**？

系统后台的 **Windows Update**、**Microsoft OneDrive 云同步**、各类软件的后台自动更新程序和后台常驻进程，常常在您毫无察觉的情况下疯狂吞噬手机流量。而 Windows 自带的任务管理器只能查看瞬时网速（$KB/s$），既无法清晰展现**累计流量的消耗百分比**，也无法提供**一键快速止血**的控制手段。

**WhoEatWifi** 致力于让您对 Wi-Fi 和热点的每一字节流量拥有绝对掌控权：
* 📊 **实时累计消耗占比 %**：一眼查明究竟是哪几个 App 瓜分了当前连接 80%+ 的宝贵流量。
* 🛡️ **一键开启按流量计费**：一键强制命令 Windows 更新、商店下载与 OneDrive 暂停所有后台传输。
* 📥 **右下角托盘静默守护**：支持关窗最小化到托盘、悬停迷你看板、超额警报即时弹出。
* 🕒 **近 1 小时网络滚动历史**：切换热点或断网重连自动归零起算，旧网络自动封存，超期自动淘汰释放。
* 🌐 **中英双语无感即时切换**：开箱默认英文，右上角一键瞬间切换为简体中文，无需重启软件。
* 🔒 **纯绿色原生单执行文件**：约 94 KB 单文件，无需安装驱动，不监听任何端口，100% 离线安全。

---

## ✨ 核心功能亮点

### 1. 📊 手机系统级直观流量占比 % 与流向分布图 (Traffic Breakdown)
- **累计消耗占比计算准则**：
  $$\text{流量消耗占比 \%} = \frac{\text{应用本次累计消耗字节}}{\text{当前网络连接累计总字节}} \times 100\%$$
- **Top 5 彩色堆叠分布看板**：位于界面顶部，以类似手机电池/流量看板的形式横向彩色堆叠展现（例如：`[ Chrome 72.5% | OneDrive 18.2% | Steam 5.1% | 其他应用 4.2% ]`），一目了然。
- **列表彩色进度条与占比徽章**：为每一个活跃应用配有实时百分比数值与进度条，大流量应用醒目高亮（> 50% 红色预警，> 20% 蓝色高亮）。
- **默认按占比降序排列**：消耗流量最多的应用始终稳居首位，绝不遗漏偷跑元凶。

### 2. 🛡️ 热点防偷跑止血与预算限额控制
- **一键切换按流量计费 (Metered Connection)**：通过 Windows 原生接口一键启用“按流量计费网络”属性，系统将正式暂停 Windows Update、应用商店后台下载及 OneDrive 自动同步。
- **热点预算超额警报**：支持设置热点限额（500 MB / 1.0 GB / 2.0 GB / 5.0 GB），一旦超出设定阈值，界面弹出醒目提示横幅、播放系统警报声并弹出系统通知。
- **一键强行结束偷跑进程 (End Task)**：直接在列表中一键切断指定恶意消耗流量的后台进程。
- **快速定位可疑程序目录**：一键在 Windows 资源管理器中打开并选中该程序的文件位置。

### 3. 📥 Windows 右下角托盘后台静默监控 (System Tray)
- **点击 [X] 自动收缩入托盘**：关闭窗口不会退出程序，而是静默收缩到屏幕右下角的箭头托盘中，持续在后台提供精准守护。
- **鼠标悬停迷你看板**：鼠标指针移至蓝色 Wi-Fi 托盘图标上方时，即刻浮现当前状态：
  ```text
  WhoEatWifi 流量卫士
  网络: iPhone_Hotspot | ⬇ 2.4 MB/s
  已用: 420.5 MB (Chrome 78.4%)
  ```
- **右键托盘快捷菜单**：支持双击唤出主面板，或右键快速唤出“显示主界面”、“切换按流量计费”、“归零重算本次流量”、“退出程序”。
- **桌面气泡通知**：在后台监控状态下，发生网络切换或流量超额时自动弹出 Windows 系统气泡提醒。

### 4. 🕒 Wi-Fi 切换自动感知与近 1 小时滚动历史
- **网络切换自动感知**：实时监测网卡与 Wi-Fi 名称（SSID）变更（例如从家中 Wi-Fi 切到手机移动热点）。
- **流量自动归零**：一旦检测到网络切换，当前统计数据自动归零重新起算，避免不同网络的流量混淆。
- **近 1 小时历史记录追溯**：切换离开的旧网络会自动归档，完整保留其连接时间段、总流量与各个 App 的消耗明细。超过 60 分钟的历史记录会自动淘汰清理，节约内存。

### 5. 🌐 中英双语即时无感切换 (Bilingual Support)
- **默认英文**：开箱默认适配国际化与专业开发者的使用习惯。
- **右上角下拉框一键切换**：主界面右上角配备 `[ English / 简体中文 ▼ ]` 下拉框。
- **零重启全屏即时刷新**：点击瞬间主界面标题、数据卡片、表头、按钮、右键托盘菜单及提示语全部实时更新。
- **自动记忆偏好**：用户的语言选择会自动保存在程序同目录下的 `settings.ini` 中，下次打开依然有效。

### 6. 🔒 100% 纯离线、隐私与零崩溃设计
- **无入站监听端口**：软件不开启任何 Web 服务或 Socket 监听端口，杜绝黑客探测与利用风险。
- **零外部网络通信**：不发送任何遥测数据、统计埋点或外部分析包，绝不泄露您的任何隐私。
- **普通用户权限运行 (`asInvoker`)**：启动时不弹出烦人的 UAC 管理员提权确认框，绿色轻量。
- **高可靠性内核级 API 封装**：采用 Windows 官方 IP Helper API (`GetExtendedTcpTable` / `GetExtendedUdpTable` / `GetPerTcpConnectionEStats`) 与 `System.Net.NetworkInformation`，告别易发生蓝屏或驱动崩溃的风险，并配有全局故障捕获机制 (`crash.log`)。

---

## 🖥️ 界面结构一览

| 界面区域 | 功能与说明 |
| :--- | :--- |
| **当前连接网络** | 实时显示当前 Wi-Fi 名称 (SSID)、无线网卡型号与计费模式状态 |
| **实时传输速率** | 大字看板指示当前全网实时下行速度 (⬇️ MB/s) 与上行速度 (⬆️ KB/s) |
| **本次连接已用流量** | 统计本次连接累计消耗的总流量、下行/上行拆分、连接时长及归零重算按钮 |
| **热点预算限额报警** | 提供热点额度下拉选项（不限额 / 500MB / 1GB / 2GB / 5GB），超额即时报警 |
| **流量占比分布看板** | 手机系统风格的横向彩色堆叠条，直观展示 Top 5 消耗应用及占比 |
| **进程网络监控网格** | 显示应用图标、进程名、PID、累计流量占比 %、进度条、实时速率与快捷操作 |
| **近 1 小时历史记录** | 左右分栏下钻查看最近 1 小时内各 Wi-Fi 会话的流量记录与各 App 明细 |
| **系统托盘** | 原生蓝色圆角 Wi-Fi 图标、鼠标悬停迷你看板与右键快捷操作菜单 |

---

## 🚀 极速上手与运行

### 1. 直接双击运行（绿色免安装）
项目已在 `bin\Release\` 目录中生成了单一原生执行文件：
> **直接双击运行 [`bin\Release\WhoEatWifi.exe`](../bin/Release/WhoEatWifi.exe) 即可立即启动使用！**

*(无安装包、无第三方运行时依赖、不修改系统注册表。您可将 `WhoEatWifi.exe` 拷贝到 U盘 或任意目录随时使用)*。

### 2. 从源码重新构建
无需安装庞大的外部 SDK，使用 Windows 自带的 .NET 构建工具即可一键构建编译：

```powershell
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe" "WhoEatWifi.csproj" /p:Configuration=Release /p:Platform=x64 /t:Rebuild
```

### 3. 运行自动化单元测试
如需验证全部核心数据统计、百分比计算算法、网络切换感知、1小时历史淘汰与双语词典：

```powershell
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe" /target:exe /out:bin\TestRunner.exe Tests\CoreTests.cs Core\SessionHistoryManager.cs Core\ProcessResolver.cs Core\LanguageManager.cs Models\AppTrafficRecord.cs Models\NetworkSession.cs Models\ProcessTrafficInfo.cs Models\TrafficSegment.cs /reference:C:\WINDOWS\Microsoft.Net\assembly\GAC_64\PresentationCore\v4.0_4.0.0.0__31bf3856ad364e35\PresentationCore.dll /reference:C:\WINDOWS\Microsoft.Net\assembly\GAC_MSIL\WindowsBase\v4.0_4.0.0.0__31bf3856ad364e35\WindowsBase.dll
& "bin\TestRunner.exe"
```
*(全部 25 项自动化单元测试均通过，0 错误)*。

---

## 🏛️ 项目目录与架构

```
d:\Github\WhoEatWifi\
├── App.xaml                    # WPF 现代化全局样式与控件模版
├── App.xaml.cs                 # 全局故障安全异常捕获与日志记录
├── App.manifest                # asInvoker 执行权限配置 (无需频繁弹 UAC)
├── MainWindow.xaml             # 现代化双标签页响应式界面布局
├── MainWindow.xaml.cs          # UI 事件分发、双语即时切换与数据绑定
├── WhoEatWifi.csproj           # 单文件纯净原生 MSBuild 工程文件
├── Core/
│   ├── IphlpNetworkMonitor.cs  # 基于 IP Helper 的高性能 TCP/UDP 扫描与 EStats 扩展
│   ├── NetworkStatsManager.cs  # 1秒高精度调度引擎、流量统计、限额监控与占比计算
│   ├── WifiHelper.cs           # 原生 WlanApi Wi-Fi 探测与按流量计费一键切换
│   ├── ProcessResolver.cs      # 进程名称解析、应用描述缓存与 WPF 图标提取
│   ├── SessionHistoryManager.cs# 网络切换感知器与近1小时历史记录滚动淘汰管理器
│   └── LanguageManager.cs      # 中英双语词典与 settings.ini 持久化存储
├── Models/
│   ├── ProcessTrafficInfo.cs   # 活跃进程流量数据绑定模型 (INotifyPropertyChanged)
│   ├── NetworkSession.cs       # 网络会话模型 (连接时长、网络名与总消耗)
│   ├── AppTrafficRecord.cs     # 历史网络下各应用消耗快照记录
│   └── TrafficSegment.cs       # 顶部彩色横向堆叠条数据结构
├── UI/
│   ├── TrayManager.cs          # Windows Forms 托盘图标、右键菜单与悬停看板
│   ├── TrayIconHelper.cs       # 基于 GDI+ 的动态原生蓝色圆角 Wi-Fi 图标绘制
│   └── ValueConverters.cs      # 速率/字节/百分比进度条颜色映射转换器
├── Tests/
│   └── CoreTests.cs            # 25 项全功能自动化单元测试套件
├── README/
│   └── README(ZH).md           # 完整简体中文文档
└── bin/Release/
    └── WhoEatWifi.exe          # 编译生成的独立单文件绿色执行程序 (~94 KB)
```

---

## 📄 开源许可证

本项目基于 [MIT 许可证](LICENSE) 开源。
