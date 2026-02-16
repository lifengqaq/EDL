using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.Layout;
using lfEDL.Qualcomm.Common;
using lfEDL.Qualcomm.Database;
using lfEDL.Qualcomm.Models;
using lfEDL.Qualcomm.Models;
using lfEDL.Qualcomm.Services;
using lfEDL.Avalonia.Dialogs;

namespace lfEDL.Avalonia
{
    public partial class MainWindow : Window
    {
        #region Fields
        private QualcommService _service;
        private DeviceInfoService _deviceInfoService;
        private CancellationTokenSource _cts;
        private bool _isBusy;
        private bool _isConnected;
        private string _connectedPortName;
        private List<PartitionInfo> _partitions;
        private DeviceBasicInfo _buildPropInfo;
        private DateTime _opStartTime;
        private long _opTotalBytes, _opDoneBytes, _stepTotalBytes, _stepDoneBytes;
        private DispatcherTimer _portMonitorTimer, _autoRefreshTimer, _opTimer;
        private StreamWriter _logWriter;
        private ObservableCollection<PartitionDisplayModel> _partitionModels = new ObservableCollection<PartitionDisplayModel>();

        // UI controls
        private TextBlock _statusText, _speedText, _timerText, _selectedPartText;
        private TextBox _logBox, _readOutputBox, _writeFileBox, _programmerBox;
        private TextBox _digestBox, _signatureBox, _chimeraBox;
        private TextBox _rawprogramBox, _bootLunBox, _flashInfoBox, _searchBox;
        private List<string> _lastPatchFiles = new List<string>();
        private StackPanel _vipAuthPanel;
        private StackPanel _digestRow, _signatureRow, _chimeraRow;
        private ProgressBar _totalProgress, _subProgress;
        private TextBlock _totalProgressText, _subProgressText;
        private ComboBox _portCombo, _storageCombo, _authCombo, _slotCombo;
        private CheckBox _skipSaharaCb, _protectCb, _generateXmlCb, _autoRebootCb, _metaSuperCb;
        private Button _readGptBtn, _refreshPortsBtn, _disconnectBtn, _quickReconnBtn;
        private Button _resetSaharaBtn, _hardResetBtn;
        private Button _readPartBtn, _writePartBtn, _erasePartBtn;
        private Button _flashSelectedBtn, _cancelBtn;
        private Button _rebootSysBtn, _rebootEdlBtn, _switchSlotBtn, _bootLunBtn;

        private DataGrid _grid;
        private TextBlock _brandText, _chipText, _serialText, _storageText, _slotText;
        private TextBlock _modelText, _androidText, _securityText, _codenameText;
        #endregion

        public MainWindow()
        {
            AvaloniaXamlLoader.Load(this);
            BindControls();
            InitUI();
            InitTimers();
            InitLogFile();
        }

        #region Init
        private void BindControls()
        {
            _statusText = this.FindControl<TextBlock>("StatusText");
            _speedText = this.FindControl<TextBlock>("SpeedText");
            _timerText = this.FindControl<TextBlock>("TimerText");
            _selectedPartText = this.FindControl<TextBlock>("SelectedPartitionText");
            _logBox = this.FindControl<TextBox>("LogBox");
            _totalProgress = this.FindControl<ProgressBar>("TotalProgress");
            _subProgress = this.FindControl<ProgressBar>("SubProgress");
            _totalProgressText = this.FindControl<TextBlock>("TotalProgressText");
            _subProgressText = this.FindControl<TextBlock>("SubProgressText");
            _grid = this.FindControl<DataGrid>("PartitionsGrid");
            _searchBox = this.FindControl<TextBox>("PartitionSearchBox");
            _readOutputBox = this.FindControl<TextBox>("ReadOutputPathBox");
            _writeFileBox = this.FindControl<TextBox>("WriteFilePathBox");
            _programmerBox = this.FindControl<TextBox>("ProgrammerPathBox");
            _digestBox = this.FindControl<TextBox>("DigestPathBox");
            _signatureBox = this.FindControl<TextBox>("SignaturePathBox");
            _chimeraBox = this.FindControl<TextBox>("ChimeraPlatformBox");
            _rawprogramBox = this.FindControl<TextBox>("RawprogramPathBox");
            _bootLunBox = this.FindControl<TextBox>("BootLunBox");
            _flashInfoBox = this.FindControl<TextBox>("FlashInfoBox");
            _vipAuthPanel = this.FindControl<StackPanel>("VipAuthPanel");
            _digestRow = this.FindControl<StackPanel>("DigestRow");
            _signatureRow = this.FindControl<StackPanel>("SignatureRow");
            _chimeraRow = this.FindControl<StackPanel>("ChimeraRow");
            _portCombo = this.FindControl<ComboBox>("PortComboBox");
            _storageCombo = this.FindControl<ComboBox>("StorageComboBox");
            _authCombo = this.FindControl<ComboBox>("AuthModeComboBox");
            _slotCombo = this.FindControl<ComboBox>("SwitchSlotComboBox");
            _skipSaharaCb = this.FindControl<CheckBox>("SkipSaharaCheckBox");
            _protectCb = this.FindControl<CheckBox>("ProtectPartitionsCheckBox");
            _generateXmlCb = this.FindControl<CheckBox>("GenerateXmlCheckBox");
            _autoRebootCb = this.FindControl<CheckBox>("AutoRebootCheckBox");
            _metaSuperCb = this.FindControl<CheckBox>("MetaSuperCheckBox");
            _readGptBtn = this.FindControl<Button>("ReadGptButton");
            _refreshPortsBtn = this.FindControl<Button>("RefreshPortsButton");
            _disconnectBtn = this.FindControl<Button>("DisconnectButton");
            _quickReconnBtn = this.FindControl<Button>("QuickReconnectButton");
            _resetSaharaBtn = this.FindControl<Button>("ResetSaharaButton");
            _hardResetBtn = this.FindControl<Button>("HardResetButton");
            _readPartBtn = this.FindControl<Button>("ReadPartitionButton");
            _writePartBtn = this.FindControl<Button>("WritePartitionButton");
            _erasePartBtn = this.FindControl<Button>("ErasePartitionButton");
            _flashSelectedBtn = this.FindControl<Button>("FlashSelectedButton");
            _cancelBtn = this.FindControl<Button>("CancelOperationButton");
            _rebootSysBtn = this.FindControl<Button>("RebootSystemButton");
            _rebootEdlBtn = this.FindControl<Button>("RebootEdlButton");
            _switchSlotBtn = this.FindControl<Button>("SwitchSlotButton");
            _bootLunBtn = this.FindControl<Button>("BootLunButton");

            var browseReadBtn = this.FindControl<Button>("BrowseReadOutputButton");
            var browseWriteBtn = this.FindControl<Button>("BrowseWriteFileButton");
            var browseProgrammerBtn = this.FindControl<Button>("BrowseProgrammerButton");
            var browseDigestBtn = this.FindControl<Button>("BrowseDigestButton");
            var browseSigBtn = this.FindControl<Button>("BrowseSignatureButton");
            var browseRawBtn = this.FindControl<Button>("BrowseRawprogramButton");

            _grid.ItemsSource = _partitionModels;

            _readGptBtn.Click += async (s, e) => await ReadGptAsync();
            _refreshPortsBtn.Click += (s, e) => RefreshPorts();
            _disconnectBtn.Click += (s, e) => Disconnect();
            _quickReconnBtn.Click += async (s, e) => await QuickReconnectAsync();
            _resetSaharaBtn.Click += async (s, e) => await ResetSaharaAsync();
            _hardResetBtn.Click += async (s, e) => await HardResetAsync();
            _readPartBtn.Click += async (s, e) => await ReadPartitionAsync();
            _writePartBtn.Click += async (s, e) => await WritePartitionAsync();
            _erasePartBtn.Click += async (s, e) => await ErasePartitionAsync();
            _flashSelectedBtn.Click += async (s, e) => await FlashSelectedAsync();
            _cancelBtn.Click += (s, e) => { _cts?.Cancel(); AppendLog("操作已取消"); };
            _rebootSysBtn.Click += async (s, e) => await RebootSystemAsync();
            _rebootEdlBtn.Click += async (s, e) => await RebootEdlAsync();
            _switchSlotBtn.Click += async (s, e) => await SwitchSlotAsync();
            _bootLunBtn.Click += async (s, e) => await BootLunAsync();

            browseReadBtn.Click += async (s, e) => { var p = await BrowseFolderAsync("选择读取输出目录"); if (p != null) _readOutputBox.Text = p; };
            browseWriteBtn.Click += async (s, e) => { var p = await BrowseFileAsync("选择写入文件", false); if (p != null) _writeFileBox.Text = p; };
            browseProgrammerBtn.Click += async (s, e) => { var p = await BrowseFileAsync("选择 Programmer/Loader", false); if (p != null) _programmerBox.Text = p; };
            browseDigestBtn.Click += async (s, e) => { var p = await BrowseFileAsync("选择 Digest 文件", false); if (p != null) _digestBox.Text = p; };
            browseSigBtn.Click += async (s, e) => { var p = await BrowseFileAsync("选择 Signature 文件", false); if (p != null) _signatureBox.Text = p; };
            browseRawBtn.Click += async (s, e) => { var p = await BrowseFileAsync("选择 Rawprogram XML", true); if (p != null) { _rawprogramBox.Text = p; await LoadRawprogramAsync(); } };
            _searchBox.TextChanged += (s, e) => UpdatePartitionFilter();
            _grid.SelectionChanged += (s, e) => UpdateSelectedPartitionInfo();
            _grid.DoubleTapped += async (s, e) => await SelectCustomFileForSelectedAsync();
            _authCombo.SelectionChanged += (s, e) => OnAuthModeChanged();
        }

        private void InitUI()
        {
            _storageCombo.ItemsSource = new[] { "ufs", "emmc" };
            _storageCombo.SelectedIndex = 0;
            _authCombo.ItemsSource = new[] { "无认证", "OPLUS", "oldOPLUS/Chimera", "小米" };
            _authCombo.SelectedIndex = 0;
            _slotCombo.ItemsSource = new[] { "a", "b" };
            _slotCombo.SelectedIndex = 0;
            _statusText.Text = "就绪";
            _readOutputBox.Text = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReadBack");
            RefreshPorts();
            OnAuthModeChanged(); // set initial visibility

            // Setup SelectAll checkbox as column header
            if (_grid.Columns.Count > 0)
            {
                var headerCb = new CheckBox { Content = null, IsThreeState = false, HorizontalAlignment = HorizontalAlignment.Center };
                headerCb.Click += (s, e) => SetAllPartitionSelection(headerCb.IsChecked == true);
                _grid.Columns[0].Header = headerCb;
            }
            ResetProgress();
        }

        private void InitTimers()
        {
            _portMonitorTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            _portMonitorTimer.Tick += (s, e) => OnPortMonitorTick();
            _autoRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _autoRefreshTimer.Tick += (s, e) => OnAutoRefreshTick();
            _autoRefreshTimer.Start();
            _opTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _opTimer.Tick += (s, e) => OnOpTimerTick();
        }

        private void InitLogFile()
        {
            try
            {
                var dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                Directory.CreateDirectory(dir);
                _logWriter = new StreamWriter(Path.Combine(dir, $"lfEDL_{DateTime.Now:yyyy-MM-dd}.log"), true, Encoding.UTF8) { AutoFlush = true };
            }
            catch { }
        }
        #endregion

        #region IsBusy
        private bool SetBusy(string op = null)
        {
            if (_isBusy) { AppendLog("操作进行中，请等待"); return false; }
            _isBusy = true;
            _opStartTime = DateTime.Now;
            _opDoneBytes = 0;
            _opTimer.Start();
            if (op != null) _statusText.Text = op;
            return true;
        }

        private void ClearBusy()
        {
            _isBusy = false;
            _opTimer.Stop();
            _speedText.Text = "";
            _statusText.Text = _isConnected ? "已连接" : "就绪";
        }
        #endregion

        #region Port Management
        private void RefreshPorts()
        {
            var detected = PortDetector.DetectEdlPorts();
            var portNames = detected.Select(p => p.PortName).ToList();
            if (portNames.Count == 0) portNames = new List<string>(SerialPort.GetPortNames());
            _portCombo.ItemsSource = portNames;
            if (_portCombo.Items.Count > 0) _portCombo.SelectedIndex = 0;
        }

        private void StartPortMonitor(string portName)
        {
            _connectedPortName = portName;
            _portMonitorTimer.Start();
        }

        private void StopPortMonitor()
        {
            _portMonitorTimer.Stop();
            _connectedPortName = null;
        }

        private void OnPortMonitorTick()
        {
            if (string.IsNullOrEmpty(_connectedPortName) || _service == null) return;
            var ports = SerialPort.GetPortNames();
            if (!Array.Exists(ports, p => p.Equals(_connectedPortName, StringComparison.OrdinalIgnoreCase)))
            {
                AppendLog($"端口 {_connectedPortName} 已断开");
                _portMonitorTimer.Stop();
                Disconnect();
                RefreshPorts();
            }
        }

        private void OnAutoRefreshTick()
        {
            if (!_isBusy && !_isConnected) RefreshPorts();
        }

        private void OnOpTimerTick()
        {
            var elapsed = DateTime.Now - _opStartTime;
            _timerText.Text = $"耗时: {elapsed:mm\\:ss}";
            if (_opDoneBytes > 0 && elapsed.TotalSeconds > 0.5)
            {
                double speed = (_opDoneBytes / 1024.0 / 1024.0) / elapsed.TotalSeconds;
                _speedText.Text = $"{speed:F1} MB/s";
            }
        }
        #endregion

        #region Connection
        private void OnAuthModeChanged()
        {
            int idx = _authCombo.SelectedIndex;
            // 0=无认证, 1=OPLUS(VIP), 2=oldOPLUS/Chimera, 3=小米
            switch (idx)
            {
                case 1: // OPLUS (VIP): show digest + sig, hide chimera
                    _vipAuthPanel.IsVisible = true;
                    _digestRow.IsVisible = true;
                    _signatureRow.IsVisible = true;
                    _chimeraRow.IsVisible = false;
                    break;
                case 2: // oldOPLUS/Chimera: show only chimera platform
                    _vipAuthPanel.IsVisible = true;
                    _digestRow.IsVisible = false;
                    _signatureRow.IsVisible = false;
                    _chimeraRow.IsVisible = true;
                    break;
                default: // 0=无认证, 3=小米: hide entire panel
                    _vipAuthPanel.IsVisible = false;
                    break;
            }
        }

        private async Task ConnectAndAuthenticateAsync()
        {
            string portName = _portCombo.SelectedItem as string;
            if (string.IsNullOrEmpty(portName)) { AppendLog("请选择端口"); return; }

            bool skipSahara = _skipSaharaCb.IsChecked == true;
            string programmer = _programmerBox.Text?.Trim() ?? "";

            if (!skipSahara)
            {
                if (string.IsNullOrEmpty(programmer)) { AppendLog("请选择 Programmer/Loader 文件"); return; }
                if (!File.Exists(programmer)) { AppendLog("Programmer 文件不存在"); return; }
            }

            EnsureService();
            _cts = new CancellationTokenSource();
            string storageType = _storageCombo.SelectedItem as string ?? "ufs";
            int authMode = _authCombo.SelectedIndex;

            // Map UI auth index to ConnectAsync authMode string
            // 0=none, 1=vip (OPLUS VIP), 2=none (chimera done post-connect), 3=xiaomi
            string authModeStr;
            switch (authMode)
            {
                case 1: authModeStr = "vip"; break;
                case 3: authModeStr = "xiaomi"; break;
                default: authModeStr = "none"; break;
            }
            string digestPath = _digestBox.Text?.Trim() ?? "";
            string sigPath = _signatureBox.Text?.Trim() ?? "";

            try
            {
                _statusText.Text = "连接中...";
                bool connected;
                if (skipSahara)
                    connected = await _service.ConnectFirehoseDirectAsync(portName, storageType, _cts.Token);
                else
                    connected = await _service.ConnectAsync(portName, programmer, storageType, authModeStr, digestPath, sigPath, _cts.Token);

                if (!connected) { AppendLog("连接失败"); _statusText.Text = "连接失败"; return; }

                _isConnected = true;
                _statusText.Text = "已连接";
                StartPortMonitor(portName);
                AppendLog("连接成功");

                // Post-connect Chimera auth (index 2 = oldOPLUS/Chimera)
                if (authMode == 2)
                {
                    await PerformChimeraAuthAsync();
                }

                UpdateDeviceInfo();
            }
            catch (OperationCanceledException) { AppendLog("连接已取消"); }
            catch (Exception ex) { AppendLog($"连接异常: {ex.Message}"); }
        }

        private async Task QuickReconnectAsync()
        {
            if (!SetBusy("快速重连中...")) return;
            try
            {
                _cts = new CancellationTokenSource();
                string port = _connectedPortName ?? _portCombo.SelectedItem as string;
                if (string.IsNullOrEmpty(port)) { AppendLog("请选择端口"); ClearBusy(); return; }
                string storageType = _storageCombo.SelectedItem as string ?? "ufs";
                bool ok = await _service.ConnectFirehoseDirectAsync(port, storageType, _cts.Token);
                if (ok) { _isConnected = true; StartPortMonitor(port); }
                AppendLog(ok ? "快速重连成功" : "快速重连失败");
            }
            catch (Exception ex) { AppendLog($"快速重连异常: {ex.Message}"); }
            finally { ClearBusy(); }
        }

        private void Disconnect()
        {
            try
            {
                StopPortMonitor();
                _service?.Disconnect();
                _isConnected = false;
                _partitions = null;
                _partitionModels.Clear();
                _buildPropInfo = null;
                SetDeviceInfoPlaceholders();
                _statusText.Text = "已断开";
                _skipSaharaCb.IsChecked = false;
                AppendLog("已断开连接");
            }
            catch (Exception ex)
            {
                AppendLog($"断开异常: {ex.Message}");
            }
        }

        private async Task ResetSaharaAsync()
        {
            string port = _portCombo.SelectedItem as string;
            if (string.IsNullOrEmpty(port)) { AppendLog("请选择端口"); return; }
            if (!SetBusy("重置 Sahara...")) return;
            try
            {
                EnsureService();
                _cts = new CancellationTokenSource();
                bool ok = await _service.ResetSaharaAsync(port, _cts.Token);
                AppendLog(ok ? "Sahara 重置成功，请重新连接" : "Sahara 重置失败");
                if (ok) { _skipSaharaCb.IsChecked = false; Disconnect(); RefreshPorts(); }
            }
            catch (Exception ex) { AppendLog($"重置异常: {ex.Message}"); }
            finally { ClearBusy(); }
        }

        private async Task HardResetAsync()
        {
            string port = _portCombo.SelectedItem as string;
            if (string.IsNullOrEmpty(port)) { AppendLog("请选择端口"); return; }
            if (!SetBusy("硬重置设备...")) return;
            try
            {
                EnsureService();
                _cts = new CancellationTokenSource();
                bool ok = await _service.HardResetDeviceAsync(port, _cts.Token);
                AppendLog(ok ? "设备正在重启" : "硬重置失败");
                if (ok) { Disconnect(); await Task.Delay(2000); RefreshPorts(); }
            }
            catch (Exception ex) { AppendLog($"硬重置异常: {ex.Message}"); }
            finally { ClearBusy(); }
        }
        #endregion

        #region Service Helpers
        private void EnsureService()
        {
            if (_service != null) return;
            _service = new QualcommService(
                msg => Dispatcher.UIThread.Post(() => AppendLog(msg)), null, msg => { });

            _service.XiaomiSignatureProvider = GetXiaomiSignatureAsync;

            _service.PortDisconnected += (s, e) => Dispatcher.UIThread.Post(() =>
            {
                AppendLog("端口断开");
                _isConnected = false;
                StopPortMonitor();
                SetDeviceInfoPlaceholders();
                _statusText.Text = "已断开";
            });
        }

        private async Task<string> GetXiaomiSignatureAsync(string token)
        {
            return await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                var dlg = new Dialogs.XiaomiAuthDialog(token);
                // Set owner to this window
                var signature = await dlg.ShowDialog<string>(this);
                return signature;
            });
        }

        private bool EnsureConnected()
        {
            if (_isConnected && _service != null) return true;
            AppendLog("请先连接设备");
            return false;
        }

        private async Task<bool> EnsureConnectedAsync()
        {
            if (!EnsureConnected()) return false;
            if (_service != null)
            {
                _cts = new CancellationTokenSource();
                return await _service.EnsurePortOpenAsync(_cts.Token);
            }
            return false;
        }
        #endregion

        #region Partition Reading
        private async Task ReadGptAsync()
        {
            if (_isConnected && _service != null)
            {
                if (!SetBusy("读取分区表...")) return;
            }
            else
            {
                await ConnectAndAuthenticateAsync();
                if (!_isConnected) return;
                if (!SetBusy("读取分区表...")) return;
            }
            try
            {
                _cts = new CancellationTokenSource();
                ResetProgress();
                var totalProg = new Progress<Tuple<int, int>>(t => UpdateTotalProgress((long)t.Item1, (long)t.Item2));
                var subProg = new Progress<double>(p => UpdateSubProgress(p, 100));
                var partitions = await _service.ReadAllGptAsync(6, totalProg, subProg, _cts.Token);
                if (partitions != null && partitions.Count > 0)
                {
                    _partitions = partitions;
                    UpdatePartitionListView(partitions);
                    AppendLog($"成功读取 {partitions.Count} 个分区");
                    _skipSaharaCb.IsChecked = true;
                    UpdateDeviceInfo();
                    // Try reading build.prop
                    await TryReadBuildPropAsync();
                }
                else AppendLog("未读取到分区");
            }
            catch (OperationCanceledException) { AppendLog("读取已取消"); }
            catch (Exception ex) { AppendLog($"读取分区表失败: {ex.Message}"); }
            finally { _service?.ReleasePort(); ClearBusy(); }
        }

        private async Task ReadPartitionAsync()
        {
            if (!EnsureConnected()) return;
            var selected = GetSelectedPartitions();
            if (selected.Count == 0) { AppendLog("请选择分区"); return; }
            if (selected.Count > 1) { await ReadPartitionsBatchAsync(selected); return; }

            if (!SetBusy("读取分区...")) return;
            try
            {
                var p = selected[0];
                string dir = _readOutputBox.Text?.Trim() ?? "";
                if (string.IsNullOrEmpty(dir)) dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReadBack");
                Directory.CreateDirectory(dir);
                string outPath = Path.Combine(dir, p.Name + ".img");

                _cts = new CancellationTokenSource();
                _stepTotalBytes = p.SizeBytes; _stepDoneBytes = 0;
                _opTotalBytes = p.SizeBytes;
                var progress = new Progress<double>(pct => { _stepDoneBytes = (long)(pct / 100.0 * _stepTotalBytes); _opDoneBytes = _stepDoneBytes; UpdateSubProgress(_stepDoneBytes, _stepTotalBytes); });
                bool ok = await _service.ReadPartitionAsync(p.Name, outPath, progress, _cts.Token);
                AppendLog(ok ? $"读取成功: {outPath}" : $"读取失败: {p.Name}");
            }
            catch (OperationCanceledException) { AppendLog("读取已取消"); }
            catch (Exception ex) { AppendLog($"读取异常: {ex.Message}"); }
            finally { _service?.ReleasePort(); ClearBusy(); }
        }

        private async Task ReadPartitionsBatchAsync(List<PartitionDisplayModel> parts)
        {
            if (!SetBusy("批量读取...")) return;
            try
            {
                string dir = _readOutputBox.Text?.Trim() ?? "";
                if (string.IsNullOrEmpty(dir)) dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReadBack");
                Directory.CreateDirectory(dir);

                _cts = new CancellationTokenSource();
                _opTotalBytes = parts.Sum(p => p.SizeBytes);
                _opDoneBytes = 0;
                int success = 0;

                for (int i = 0; i < parts.Count; i++)
                {
                    if (_cts.Token.IsCancellationRequested) break;
                    var p = parts[i];
                    _stepTotalBytes = p.SizeBytes; _stepDoneBytes = 0;
                    UpdateTotalProgress(_opDoneBytes, _opTotalBytes);
                    AppendLog($"[{i + 1}/{parts.Count}] 读取 {p.Name}...");

                    string outPath = Path.Combine(dir, p.Name + ".img");
                    var progress = new Progress<double>(pct => { _stepDoneBytes = (long)(pct / 100.0 * _stepTotalBytes); UpdateSubProgress(_stepDoneBytes, _stepTotalBytes); });
                    bool ok = await _service.ReadPartitionAsync(p.Name, outPath, progress, _cts.Token);
                    if (ok) { success++; _opDoneBytes += p.SizeBytes; }
                    else AppendLog($"[失败] {p.Name}");
                }
                UpdateTotalProgress(_opTotalBytes, _opTotalBytes);
                AppendLog($"批量读取完成: {success}/{parts.Count} 成功");
            }
            catch (Exception ex) { AppendLog($"批量读取异常: {ex.Message}"); }
            finally { _service?.ReleasePort(); ClearBusy(); }
        }
        #endregion

        #region Partition Writing
        private async Task WritePartitionAsync()
        {
            if (!EnsureConnected()) return;
            var selected = GetSelectedPartitions();
            if (selected.Count == 0) { AppendLog("请选择分区"); return; }

            // MetaSuper check
            if (_metaSuperCb.IsChecked == true && selected.Count == 1 && selected[0].Name.Equals("super", StringComparison.OrdinalIgnoreCase))
            {
                var folder = await BrowseFolderAsync("选择 OPLUS 固件根目录 (含 IMAGES 和 META)");
                if (folder != null) await FlashOplusSuperAsync(folder);
                return;
            }

            if (!SetBusy("写入分区...")) return;
            try
            {
                _cts = new CancellationTokenSource();
                if (selected.Count == 1)
                {
                    var p = selected[0];
                    string file = !string.IsNullOrEmpty(p.CustomFileName) && File.Exists(p.Location) ? p.Location : _writeFileBox.Text?.Trim();
                    if (string.IsNullOrEmpty(file) || !File.Exists(file)) { file = await BrowseFileAsync($"选择写入 {p.Name} 的镜像", false); }
                    if (string.IsNullOrEmpty(file)) { ClearBusy(); return; }

                    _stepTotalBytes = new FileInfo(file).Length; _opTotalBytes = _stepTotalBytes;
                    var progress = new Progress<double>(pct => { _stepDoneBytes = (long)(pct / 100.0 * _stepTotalBytes); _opDoneBytes = _stepDoneBytes; UpdateSubProgress(_stepDoneBytes, _stepTotalBytes); });
                    bool ok = await _service.WritePartitionAsync(p.Name, file, progress, _cts.Token);
                    AppendLog(ok ? $"写入成功: {p.Name}" : $"写入失败: {p.Name}");
                    if (ok && _autoRebootCb.IsChecked == true) { AppendLog("写入完成，自动重启..."); await _service.RebootAsync(_cts.Token); }
                }
                else
                {
                    // Batch write from custom files
                    var tasks = new List<Tuple<string, string, int, long>>();
                    foreach (var p in selected)
                    {
                        if (string.IsNullOrEmpty(p.Location) || !File.Exists(p.Location)) { AppendLog($"跳过 {p.Name}: 无文件"); continue; }
                        if (_protectCb.IsChecked == true && RawprogramParser.IsSensitivePartition(p.Name)) { AppendLog($"跳过敏感分区: {p.Name}"); continue; }
                        tasks.Add(Tuple.Create(p.Name, p.Location, p.Lun, (long)p.StartSector));
                    }
                    if (tasks.Count > 0) await FlashMultipleAsync(tasks);
                }
            }
            catch (OperationCanceledException) { AppendLog("写入已取消"); }
            catch (Exception ex) { AppendLog($"写入异常: {ex.Message}"); }
            finally { _service?.ReleasePort(); ClearBusy(); }
        }
        #endregion

        #region Partition Erasing
        private async Task ErasePartitionAsync()
        {
            if (!EnsureConnected()) return;
            var selected = GetSelectedPartitions();
            if (selected.Count == 0) { AppendLog("请选择分区"); return; }
            if (!SetBusy("擦除分区...")) return;
            try
            {
                _cts = new CancellationTokenSource();
                int success = 0;
                for (int i = 0; i < selected.Count; i++)
                {
                    if (_cts.Token.IsCancellationRequested) break;
                    var p = selected[i];
                    if (_protectCb.IsChecked == true && RawprogramParser.IsSensitivePartition(p.Name)) { AppendLog($"跳过敏感分区: {p.Name}"); continue; }
                    UpdateTotalProgress(i, selected.Count);
                    bool ok = await _service.ErasePartitionAsync(p.Name, _cts.Token);
                    if (ok) success++;
                    AppendLog(ok ? $"[{i + 1}/{selected.Count}] {p.Name} 擦除成功" : $"[{i + 1}/{selected.Count}] {p.Name} 擦除失败");
                }
                UpdateTotalProgress(selected.Count, selected.Count);
                AppendLog($"擦除完成: {success}/{selected.Count} 成功");
            }
            catch (Exception ex) { AppendLog($"擦除异常: {ex.Message}"); }
            finally { _service?.ReleasePort(); ClearBusy(); }
        }
        #endregion

        #region Flash Operations
        private async Task LoadRawprogramAsync()
        {
            await Task.Yield();
            string paths = _rawprogramBox.Text?.Trim();
            if (string.IsNullOrEmpty(paths)) { AppendLog("请选择 Rawprogram XML 文件"); return; }
            try
            {
                var xmlFiles = paths.Split(new[] { ';', '|' }, StringSplitOptions.RemoveEmptyEntries);
                _partitionModels.Clear();
                foreach (var xmlPath in xmlFiles)
                {
                    if (!File.Exists(xmlPath.Trim())) { AppendLog($"文件不存在: {xmlPath}"); continue; }
                    LoadRawprogramCore(xmlPath.Trim());
                }
                AppendLog($"解析完成，共 {_partitionModels.Count} 个分区");

                // Default sort: by partition name (alpha), then LUN (numeric)
                var sorted = _partitionModels.OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase).ThenBy(p => p.Lun).ToList();
                _partitionModels.Clear();
                foreach (var item in sorted) _partitionModels.Add(item);

                // Auto-discover patch*.xml files in same directory
                _lastPatchFiles.Clear();
                var firstPath = xmlFiles.FirstOrDefault()?.Trim();
                if (!string.IsNullOrEmpty(firstPath))
                {
                    string baseDir = Path.GetDirectoryName(firstPath) ?? "";
                    if (Directory.Exists(baseDir))
                    {
                        var patchFiles = Directory.GetFiles(baseDir, "patch*.xml", SearchOption.AllDirectories)
                            .OrderBy(f => f).ToList();
                        _lastPatchFiles.AddRange(patchFiles);
                        if (patchFiles.Count > 0)
                            AppendLog($"发现 {patchFiles.Count} 个 Patch 文件，刷写时将自动应用");
                    }
                }

                ShowFlashInfo();
                if (_generateXmlCb.IsChecked == true) GenerateXmlForPartitions();
            }
            catch (Exception ex) { AppendLog($"解析失败: {ex.Message}"); }
        }

        private void LoadRawprogramCore(string xmlPath)
        {
            string dir = Path.GetDirectoryName(xmlPath) ?? "";
            var parser = new RawprogramParser(dir, msg => AppendLog(msg));
            var entries = parser.ParseRawprogramXml(xmlPath);
            foreach (var entry in entries)
            {
                string fileName = entry.Filename ?? "";
                if (string.IsNullOrEmpty(fileName) || entry.NumSectors <= 0) continue;
                string fullPath = string.IsNullOrEmpty(dir) ? fileName : Path.Combine(dir, fileName);
                bool exists = File.Exists(fullPath);
                var model = new PartitionDisplayModel
                {
                    IsSelected = exists,
                    Lun = entry.Lun,
                    Name = entry.Label ?? Path.GetFileNameWithoutExtension(fileName),
                    FormattedSize = FormatSize(entry.NumSectors * entry.SectorSize),
                    StartSector = entry.StartSector,
                    NumSectors = entry.NumSectors,
                    SizeBytes = entry.NumSectors * entry.SectorSize,
                    SectorSize = (int)entry.SectorSize,
                    CustomFileName = Path.GetFileName(fileName),
                    Location = fullPath
                };
                _partitionModels.Add(model);
            }
        }

        private async Task FlashSelectedAsync()
        {
            if (!EnsureConnected()) return;
            var selected = GetSelectedPartitions().Where(p => !string.IsNullOrEmpty(p.Location) && File.Exists(p.Location)).ToList();
            if (selected.Count == 0) { AppendLog("无可刷入的分区（请确保文件存在）"); return; }
            if (!SetBusy("批量刷写...")) return;
            try
            {
                _cts = new CancellationTokenSource();
                var tasks = new List<Tuple<string, string, int, long>>();
                foreach (var p in selected)
                {
                    if (_protectCb.IsChecked == true && RawprogramParser.IsSensitivePartition(p.Name)) { AppendLog($"跳过敏感分区: {p.Name}"); continue; }
                    tasks.Add(Tuple.Create(p.Name, p.Location, p.Lun, (long)p.StartSector));
                }
                if (tasks.Count > 0) await FlashMultipleAsync(tasks);

                // Auto-apply patch files if discovered
                if (_lastPatchFiles.Count > 0 && !_cts.Token.IsCancellationRequested)
                {
                    await ApplyPatchFilesAsync(_lastPatchFiles);
                }

                if (_autoRebootCb.IsChecked == true) { AppendLog("刷写完成，自动重启..."); await _service.RebootAsync(_cts.Token); }
            }
            catch (Exception ex) { AppendLog($"刷写异常: {ex.Message}"); }
            finally { _service?.ReleasePort(); ClearBusy(); }
        }

        private async Task FlashMultipleAsync(List<Tuple<string, string, int, long>> tasks)
        {
            _opTotalBytes = tasks.Sum(t => { try { return new FileInfo(t.Item2).Length; } catch { return 0L; } });
            _opDoneBytes = 0;
            AppendLog($"开始批量刷写 {tasks.Count} 个分区 (总计 {_opTotalBytes / 1024 / 1024}MB)...");

            for (int i = 0; i < tasks.Count; i++)
            {
                if (_cts.Token.IsCancellationRequested) break;
                var t = tasks[i];
                long fileSize = 0;
                try { fileSize = new FileInfo(t.Item2).Length; } catch { }
                _stepTotalBytes = fileSize; _stepDoneBytes = 0;
                UpdateTotalProgress(_opDoneBytes, _opTotalBytes);
                AppendLog($"[{i + 1}/{tasks.Count}] 写入 {t.Item1}...");

                var progress = new Progress<double>(pct => { _stepDoneBytes = (long)(pct / 100.0 * _stepTotalBytes); UpdateSubProgress(_stepDoneBytes, _stepTotalBytes); });
                bool ok;
                if (_partitions != null && _partitions.Exists(p => p.Name == t.Item1))
                    ok = await _service.WritePartitionAsync(t.Item1, t.Item2, progress, _cts.Token);
                else
                    ok = await _service.WriteDirectAsync(t.Item1, t.Item2, t.Item3, t.Item4, progress, _cts.Token);

                if (ok) { _opDoneBytes += fileSize; AppendLog($"[成功] {t.Item1}"); }
                else AppendLog($"[失败] {t.Item1}");
            }
            UpdateTotalProgress(_opTotalBytes, _opTotalBytes);
            AppendLog("批量刷写完成");
        }

        private async Task FlashOplusSuperAsync(string firmwareRoot)
        {
            if (!EnsureConnected()) return;
            if (!Directory.Exists(firmwareRoot)) { AppendLog("固件目录不存在"); return; }
            if (!SetBusy("OPLUS Super 写入...")) return;
            try
            {
                _cts = new CancellationTokenSource();
                string nvId = _buildPropInfo?.OplusNvId ?? "";
                var progress = new Progress<double>(p => UpdateSubProgress((long)(p / 100.0 * _stepTotalBytes), _stepTotalBytes));
                bool ok = await _service.FlashOplusSuperAsync(firmwareRoot, nvId, progress, _cts.Token);
                AppendLog(ok ? "OPLUS Super 写入完成" : "OPLUS Super 写入失败");
            }
            catch (Exception ex) { AppendLog($"OPLUS Super 异常: {ex.Message}"); }
            finally { ClearBusy(); }
        }

        private async Task ApplyPatchFilesAsync(List<string> patchFiles)
        {
            if (!EnsureConnected()) return;
            if (patchFiles == null || patchFiles.Count == 0) { AppendLog("无 Patch 文件"); return; }
            AppendLog($"开始应用 {patchFiles.Count} 个 Patch 文件...");
            try
            {
                int totalPatches = 0;
                for (int i = 0; i < patchFiles.Count; i++)
                {
                    if (_cts.Token.IsCancellationRequested) break;
                    string f = patchFiles[i];
                    if (!File.Exists(f)) { AppendLog($"Patch 文件不存在: {f}"); continue; }
                    int applied = await _service.ApplyPatchXmlAsync(f, _cts.Token);
                    totalPatches += applied;
                    AppendLog($"[{i + 1}/{patchFiles.Count}] {Path.GetFileName(f)}: {applied} 个补丁");
                }
                AppendLog($"Patch 完成: 共 {totalPatches} 个补丁");
            }
            catch (Exception ex) { AppendLog($"Patch 异常: {ex.Message}"); }
        }
        #endregion

        #region Authentication
        private async Task PerformVipAuthAsync()
        {
            if (!EnsureConnected()) return;
            string digest = _digestBox.Text?.Trim();
            string sig = _signatureBox.Text?.Trim();
            if (string.IsNullOrEmpty(digest) || string.IsNullOrEmpty(sig)) { AppendLog("请选择 Digest 和 Signature 文件"); return; }
            if (!File.Exists(digest) || !File.Exists(sig)) { AppendLog("文件不存在"); return; }
            try
            {
                _cts = new CancellationTokenSource();
                var digestData = await Task.Run(() => File.ReadAllBytes(digest));
                var sigData = await Task.Run(() => File.ReadAllBytes(sig));
                bool ok = await _service.PerformVipAuthAsync(digestData, sigData, _cts.Token);
                AppendLog(ok ? "认证成功" : "认证失败");
            }
            catch (Exception ex) { AppendLog($"认证异常: {ex.Message}"); }
        }

        private async Task PerformChimeraAuthAsync()
        {
            if (!EnsureConnected()) return;
            try
            {
                _cts = new CancellationTokenSource();
                string platform = _chimeraBox.Text?.Trim();
                bool ok;
                if (!string.IsNullOrEmpty(platform))
                    ok = await _service.PerformChimeraAuthAsync(platform, _cts.Token);
                else
                    ok = await _service.PerformChimeraAuthAutoAsync(_cts.Token);
                AppendLog(ok ? "Chimera 认证成功" : "Chimera 认证失败");
            }
            catch (Exception ex) { AppendLog($"Chimera 认证异常: {ex.Message}"); }
        }
        #endregion

        #region Advanced Operations
        private async Task RebootSystemAsync()
        {
            if (!EnsureConnected()) return;
            try { _cts = new CancellationTokenSource(); await _service.RebootAsync(_cts.Token); AppendLog("已发送重启命令"); }
            catch (Exception ex) { AppendLog($"重启异常: {ex.Message}"); }
        }

        private async Task RebootEdlAsync()
        {
            if (!EnsureConnected()) return;
            try { _cts = new CancellationTokenSource(); await _service.RebootToEdlAsync(_cts.Token); AppendLog("已发送 EDL 重启命令"); }
            catch (Exception ex) { AppendLog($"EDL 重启异常: {ex.Message}"); }
        }

        private async Task SwitchSlotAsync()
        {
            if (!EnsureConnected()) return;
            string slot = _slotCombo.SelectedItem as string ?? "a";
            try { _cts = new CancellationTokenSource(); bool ok = await _service.SetActiveSlotAsync(slot, _cts.Token); AppendLog(ok ? $"槽位已切换到 {slot}" : "槽位切换失败"); }
            catch (Exception ex) { AppendLog($"槽位切换异常: {ex.Message}"); }
        }

        private async Task BootLunAsync()
        {
            if (!EnsureConnected()) return;
            if (!int.TryParse(_bootLunBox.Text?.Trim(), out int lun)) { AppendLog("请输入有效的 LUN 编号"); return; }
            try { _cts = new CancellationTokenSource(); bool ok = await _service.SetBootLunAsync(lun, _cts.Token); AppendLog(ok ? $"Boot LUN 已设置为 {lun}" : "Boot LUN 设置失败"); }
            catch (Exception ex) { AppendLog($"Boot LUN 异常: {ex.Message}"); }
        }
        #endregion

        #region Build.prop Reading
        private async Task TryReadBuildPropAsync()
        {
            if (_partitions == null || _service == null) return;
            bool hasSuper = _partitions.Exists(p => p.Name.Equals("super", StringComparison.OrdinalIgnoreCase));
            if (!hasSuper) { AppendLog("无 super 分区，跳过 build.prop 读取"); return; }

            try
            {
                AppendLog("正在从设备读取 build.prop...");
                int sectorSize = _service.SectorSize > 0 ? _service.SectorSize : 4096;

                // Create synchronous read delegate for PartitionBuildPropReader
                PartitionBuildPropReader.PartitionReadDelegate readDel = (partName, offset, size) =>
                {
                    try
                    {
                        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
                        {
                            var task = _service.ReadPartitionDataAsync(partName, offset, size, cts.Token);
                            task.Wait(cts.Token);
                            return task.Result;
                        }
                    }
                    catch { return null; }
                };

                // First parse LP metadata to get logical partitions
                if (_deviceInfoService == null)
                    _deviceInfoService = new DeviceInfoService(msg => { }, msg => { });

                var superPart = _partitions.Find(p => p.Name.Equals("super", StringComparison.OrdinalIgnoreCase));
                long superStart = superPart != null ? (long)superPart.StartSector : 0;

                // DeviceReadDelegate reads from super partition: (offsetInSuper, size) => byte[]
                DeviceInfoService.DeviceReadDelegate lpReadDel = (offsetInSuper, size) =>
                {
                    try
                    {
                        using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15)))
                        {
                            var task = _service.ReadPartitionDataAsync("super", offsetInSuper, size, cts.Token);
                            task.Wait(cts.Token);
                            return task.Result;
                        }
                    }
                    catch { return null; }
                };

                var lpPartitions = _deviceInfoService.ParseLpMetadataFromDevice(lpReadDel, superStart, sectorSize);
                if (lpPartitions == null || lpPartitions.Count == 0) { AppendLog("LP Metadata 解析失败"); return; }

                AppendLog($"发现 {lpPartitions.Count} 个逻辑分区");

                // Create reader and read build.prop from logical partitions
                var reader = new PartitionBuildPropReader(readDel, sectorSize, msg => Dispatcher.UIThread.Post(() => AppendLog(msg)));
                string slot = _service.CurrentSlot ?? "";
                var lpNames = lpPartitions.Select(p => p.Name).ToList();
                string vendor = DetectDeviceVendor();

                var props = await Task.Run(() => reader.SmartReadBuildProp(lpNames, slot, vendor));

                if (props != null && props.Count > 0)
                {
                    _buildPropInfo = BuildPropExtractor.ExtractBasicInfo(props);
                    UpdateDeviceInfoFromBuildProp();
                    AppendLog($"build.prop 读取成功: {_buildPropInfo.DisplayName} Android {_buildPropInfo.AndroidVersion}");
                }
                else AppendLog("未能从 build.prop 获取设备信息");
            }
            catch (Exception ex) { AppendLog($"build.prop 读取失败: {ex.Message}"); }
        }

        private string DetectDeviceVendor()
        {
            if (_service == null) return "";
            try
            {
                string pkHash = _service.GetChipInfo()?.ToString() ?? "";
                string vendor = QualcommDatabase.GetVendorByPkHash(pkHash);
                if (!string.IsNullOrEmpty(vendor)) return vendor;
            }
            catch { }
            if (_partitions != null)
            {
                if (_partitions.Exists(p => p.Name.StartsWith("my_manifest"))) return "oplus";
                if (_partitions.Exists(p => p.Name == "cust")) return "xiaomi";
            }
            return "";
        }
        #endregion

        #region Device Info
        private void UpdateDeviceInfo()
        {
            if (_service == null) { SetDeviceInfoPlaceholders(); return; }
            try
            {
                if (_deviceInfoService == null) _deviceInfoService = new DeviceInfoService(msg => { }, msg => { });
                var info = _deviceInfoService.GetInfoFromQualcommService(_service);
                _brandText.Text = info?.Brand ?? "-";
                _chipText.Text = info?.ChipName ?? "-";
                _serialText.Text = info?.ChipSerial ?? "-";
                _storageText.Text = !string.IsNullOrEmpty(info?.StorageType) ? info.StorageType.ToUpper() : "-";
                _slotText.Text = info?.CurrentSlot ?? "-";
                _slotText.Text = info?.CurrentSlot ?? "-";
                // _vipText removed
            }
            catch { SetDeviceInfoPlaceholders(); }
        }

        private void UpdateDeviceInfoFromBuildProp()
        {
            if (_buildPropInfo == null) return;
            if (!string.IsNullOrEmpty(_buildPropInfo.Brand)) _brandText.Text = _buildPropInfo.Brand;
            _modelText.Text = !string.IsNullOrEmpty(_buildPropInfo.MarketName) ? _buildPropInfo.MarketName : _buildPropInfo.Model;
            _androidText.Text = _buildPropInfo.AndroidVersion;
            _securityText.Text = _buildPropInfo.SecurityPatch;
            _codenameText.Text = _buildPropInfo.Device;
        }

        private void SetDeviceInfoPlaceholders()
        {
            _brandText.Text = _chipText.Text = _serialText.Text = "-";
            _storageText.Text = _slotText.Text = "-";
            _modelText.Text = _androidText.Text = _securityText.Text = _codenameText.Text = "-";
        }
        #endregion

        #region Progress Display
        private void ResetProgress()
        {
            _opTotalBytes = _opDoneBytes = _stepTotalBytes = _stepDoneBytes = 0;
            _totalProgress.Value = 0; _subProgress.Value = 0;
            _totalProgressText.Text = "0MB/0MB 0%"; _subProgressText.Text = "0MB/0MB 0%";
            _speedText.Text = "0 MB/s"; _timerText.Text = "00:00:00";
        }

        private void UpdateTotalProgress(long done, long total)
        {
            Dispatcher.UIThread.Post(() =>
            {
                double pct = total > 0 ? 100.0 * done / total : 0;
                _totalProgress.Value = pct;
                long doneMB = done / (1024 * 1024), totalMB = total / (1024 * 1024);
                _totalProgressText.Text = total > 0 ? $"{doneMB}MB/{totalMB}MB {pct:F0}%" : "";
            });
        }

        private void UpdateSubProgress(long done, long total)
        {
            Dispatcher.UIThread.Post(() =>
            {
                double pct = total > 0 ? 100.0 * done / total : 0;
                _subProgress.Value = pct;
                long doneMB = done / (1024 * 1024), totalMB = total / (1024 * 1024);
                _subProgressText.Text = total > 0 ? $"{doneMB}MB/{totalMB}MB {pct:F0}%" : "";
            });
        }

        private void UpdateSubProgress(double done, double total)
        {
            UpdateSubProgress((long)done, (long)total);
        }
        #endregion

        #region Logging
        private void AppendLog(string message)
        {
            string line = $"[{DateTime.Now:HH:mm:ss}] {message}";
            Dispatcher.UIThread.Post(() =>
            {
                if (_logBox.Text?.Length > 50000) _logBox.Text = _logBox.Text.Substring(_logBox.Text.Length - 30000);
                _logBox.Text += line + Environment.NewLine;
                _logBox.CaretIndex = _logBox.Text.Length;
            });
            try { _logWriter?.WriteLine(line); } catch { }
        }
        #endregion

        #region UI Helpers
        private List<PartitionDisplayModel> GetSelectedPartitions()
        {
            var list = _partitionModels.Where(p => p.IsSelected).ToList();
            if (list.Count == 0 && _grid.SelectedItem is PartitionDisplayModel sel) list.Add(sel);
            return list;
        }

        private void SetAllPartitionSelection(bool selected)
        {
            foreach (var p in _partitionModels) p.IsSelected = selected;
        }

        private void UpdatePartitionFilter()
        {
            string filter = _searchBox.Text?.Trim()?.ToLower() ?? "";
            if (string.IsNullOrEmpty(filter)) { _grid.ItemsSource = _partitionModels; return; }
            var filtered = new ObservableCollection<PartitionDisplayModel>(_partitionModels.Where(p => p.Name.ToLower().Contains(filter)));
            _grid.ItemsSource = filtered;
        }

        private void UpdateSelectedPartitionInfo()
        {
            if (_grid.SelectedItem is PartitionDisplayModel p)
                _selectedPartText.Text = $"当前: {p.Name} (LUN {p.Lun}, {p.FormattedSize})";
        }

        private async Task SelectCustomFileForSelectedAsync()
        {
            if (_grid.SelectedItem is PartitionDisplayModel p)
            {
                var file = await BrowseFileAsync($"选择 {p.Name} 的镜像文件", false);
                if (file != null) { p.CustomFileName = Path.GetFileName(file); p.Location = file; }
            }
        }

        private void UpdatePartitionListView(List<PartitionInfo> partitions)
        {
            _partitionModels.Clear();
            var items = new List<PartitionDisplayModel>();
            foreach (var p in partitions)
            {
                items.Add(new PartitionDisplayModel
                {
                    Lun = p.Lun,
                    Name = p.Name,
                    FormattedSize = FormatSize(p.Size),
                    StartSector = (long)p.StartSector,
                    NumSectors = (long)p.NumSectors,
                    SizeBytes = p.Size,
                    SectorSize = (int)p.SectorSize,
                    Guid = p.TypeGuid
                });
            }
            // Default sort: by partition name (alpha), then LUN (numeric)
            foreach (var item in items.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).ThenBy(x => x.Lun))
                _partitionModels.Add(item);
        }

        private void GenerateXmlForPartitions()
        {
            try
            {
                string dir = _readOutputBox.Text?.Trim() ?? AppDomain.CurrentDomain.BaseDirectory;
                Directory.CreateDirectory(dir);
                string xmlPath = Path.Combine(dir, "rawprogram_readback.xml");
                var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<data>\n");
                foreach (var p in _partitionModels.Where(m => m.IsSelected))
                    sb.AppendLine($"  <program SECTOR_SIZE_IN_BYTES=\"{p.SectorSize}\" file_sector_offset=\"0\" filename=\"{p.Name}.img\" label=\"{p.Name}\" num_partition_sectors=\"{p.NumSectors}\" physical_partition_number=\"{p.Lun}\" start_sector=\"{p.StartSector}\" />");
                sb.AppendLine("</data>");
                File.WriteAllText(xmlPath, sb.ToString());
                AppendLog($"XML 已生成: {xmlPath}");
            }
            catch (Exception ex) { AppendLog($"XML 生成失败: {ex.Message}"); }
        }

        private void ShowFlashInfo()
        {
            var selected = _partitionModels.Where(p => p.IsSelected).ToList();
            long totalSize = selected.Sum(p => p.SizeBytes);
            _flashInfoBox.Text = $"选中 {selected.Count} 个分区, 总计 {FormatSize(totalSize)}\n"
                + string.Join("\n", selected.Select(p => $"  {p.Name}: {p.FormattedSize} -> {p.CustomFileName}"));
        }

        private async Task<string> BrowseFileAsync(string title, bool multiSelect)
        {
            var sp = GetTopLevel(this)?.StorageProvider;
            if (sp == null) return null;
            var options = new FilePickerOpenOptions { Title = title, AllowMultiple = multiSelect };
            var result = await sp.OpenFilePickerAsync(options);
            if (result == null || result.Count == 0) return null;
            if (multiSelect) return string.Join(";", result.Select(f => f.Path.LocalPath));
            return result[0].Path.LocalPath;
        }

        private async Task<string> BrowseFolderAsync(string title)
        {
            var sp = GetTopLevel(this)?.StorageProvider;
            if (sp == null) return null;
            var result = await sp.OpenFolderPickerAsync(new FolderPickerOpenOptions { Title = title });
            return result?.Count > 0 ? result[0].Path.LocalPath : null;
        }

        private string FormatSize(long bytes)
        {
            if (bytes >= 1024L * 1024 * 1024) return $"{bytes / 1024.0 / 1024.0 / 1024.0:F2} GB";
            if (bytes >= 1024 * 1024) return $"{bytes / 1024.0 / 1024.0:F2} MB";
            if (bytes >= 1024) return $"{bytes / 1024.0:F2} KB";
            return $"{bytes} B";
        }
        #endregion

        #region Cleanup
        protected override void OnClosing(WindowClosingEventArgs e)
        {
            _portMonitorTimer?.Stop();
            _autoRefreshTimer?.Stop();
            _opTimer?.Stop();
            _cts?.Cancel();
            _service?.Disconnect();
            try { _logWriter?.Flush(); _logWriter?.Dispose(); } catch { }
            base.OnClosing(e);
        }
        #endregion
    }

    public class PartitionDisplayModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private bool _isSelected;
        private string _customFileName = "";
        private string _location = "";

        public bool IsSelected { get => _isSelected; set { _isSelected = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected))); } }
        public int Lun { get; set; }
        public string Name { get; set; }
        public string FormattedSize { get; set; }
        public long StartSector { get; set; }
        public long NumSectors { get; set; }
        public string CustomFileName { get => _customFileName; set { _customFileName = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CustomFileName))); } }
        public string Location { get => _location; set { _location = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Location))); } }
        public long SizeBytes { get; set; }
        public int SectorSize { get; set; }
        public string Guid { get; set; }
    }
}

