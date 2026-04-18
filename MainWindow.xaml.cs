using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace WslBackupManager;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<DistroInfo> _distros = new();

    public MainWindow()
    {
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText(@"C:\WslBackupManager_error.log", "InitializeComponent failed: " + ex);
            throw;
        }

        try
        {
            DistroGrid.ItemsSource = _distros;

            RefreshButton.Click += async (_, _) => await LoadDistrosAsync();
            ShutdownAllButton.Click += async (_, _) => await RunSimpleCommandAsync("--shutdown", LocalizationManager.ShutdownOk);
            BackupButton.Click += async (_, _) => await BackupSelectedAsync();
            RestoreButton.Click += async (_, _) => await RestoreSelectedAsync();
            OpenBackupFolderButton.Click += (_, _) => OpenFolder(BackupRootTextBox.Text);
            OpenInstallFolderButton.Click += (_, _) => OpenFolder(InstallRootTextBox.Text);
            DistroGrid.SelectionChanged += (_, _) => SyncSelection();

            // Language selector - populate without triggering event
            LangSelector.SelectionChanged -= OnLangSelectorChanged;
            LangSelector.Items.Add(LocalizationManager.T("LangEN"));
            LangSelector.Items.Add(LocalizationManager.T("LangTC"));
            LangSelector.Items.Add(LocalizationManager.T("LangSC"));
            LangSelector.SelectedIndex = 1;
            LangSelector.SelectionChanged += OnLangSelectorChanged;

            Loaded += OnWindowLoaded;
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText(@"C:\WslBackupManager_error.log", "Constructor failed: " + ex);
            throw;
        }
    }

    private void OnLangSelectorChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        try
        {
            LocalizationManager.CurrentLang = (LocalizationManager.Language)LangSelector.SelectedIndex;
            RefreshUIText();
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText(@"C:\WslBackupManager_error.log", "OnLangSelectorChanged failed: " + ex);
        }
    }

    private async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        try
        {
            RefreshUIText();
            await LoadDistrosAsync();
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText(@"C:\WslBackupManager_error.log", "OnWindowLoaded failed: " + ex);
        }
    }

    private void RefreshUIText()
    {
        try
        {
        // Update window title and header
        TitleText.Text = LocalizationManager.AppTitle;
        SubtitleText.Text = LocalizationManager.AppSubtitle;

        // Header buttons
        RefreshButton.Content = LocalizationManager.Refresh;
        ShutdownAllButton.Content = LocalizationManager.ShutdownAll;

        // Language selector items
        var savedLang = LangSelector.SelectedIndex;
        LangSelector.Items.Clear();
        LangSelector.Items.Add(LocalizationManager.T("LangEN"));
        LangSelector.Items.Add(LocalizationManager.T("LangTC"));
        LangSelector.Items.Add(LocalizationManager.T("LangSC"));
        LangSelector.SelectedIndex = savedLang;

        // Left panel
        ConfigTitle.Text = LocalizationManager.Config;
        ConfigDesc.Text = LocalizationManager.ConfigDesc;
        BackupRootLabel.Text = LocalizationManager.BackupRoot;
        InstallRootLabel.Text = LocalizationManager.InstallRoot;
        BackupFormatLabel.Text = LocalizationManager.BackupFormat;
        SelectedDistroLabel.Text = LocalizationManager.SelectedDistro;
        BackupButton.Content = LocalizationManager.BackupNow;
        RestoreButton.Content = LocalizationManager.Restore;
        OpenBackupFolderButton.Content = LocalizationManager.OpenBackupFolder;
        OpenInstallFolderButton.Content = LocalizationManager.OpenInstallFolder;

        // Right panel
        WslListTitle.Text = LocalizationManager.WslInstances;
        WslListDesc.Text = LocalizationManager.WslInstancesDesc;

        // DataGrid column headers
        if (DistroGrid.Columns.Count >= 5)
        {
            DistroGrid.Columns[0].Header = LocalizationManager.ColName;
            DistroGrid.Columns[1].Header = LocalizationManager.ColState;
            DistroGrid.Columns[2].Header = LocalizationManager.ColVersion;
            DistroGrid.Columns[3].Header = LocalizationManager.ColLatestBackup;
            DistroGrid.Columns[4].Header = LocalizationManager.ColInstallPath;
        }

        // Refresh the distro list to re-localize state values
        var items = _distros.ToList();
        _distros.Clear();
        foreach (var item in items) _distros.Add(item);

        SetStatus(LocalizationManager.Ready);
        }
        catch (Exception ex)
        {
            System.IO.File.WriteAllText(@"C:\WslBackupManager_error.log", "RefreshUIText failed: " + ex);
            SetStatus("UI init error: " + ex.Message);
        }
    }

    private void OnLangChanged(object? sender, PropertyChangedEventArgs e)
    {
        LocalizationManager.StaticPropertyChanged -= OnLangChanged;
        RefreshUIText();
    }

    private void SyncSelection()
    {
        if (DistroGrid.SelectedItem is DistroInfo item)
            SelectedDistroTextBox.Text = item.Name;
    }

    private async Task LoadDistrosAsync()
    {
        try
        {
            SetStatus(LocalizationManager.Loading);
            var basic = await RunWslCaptureAsync("-l -q");
            var verbose = await RunWslCaptureAsync("-l -v");

            // Parse basic list - remove BOM, filter strictly
            var rawNames = basic
                .Replace("\uFEFF", "")  // Remove BOM
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x) && x.Length > 1)
                .ToList();

            // Also filter out common header-like lines (in any language)
            var headerKeywords = new[] { "name", "名稱", "名称", "NAME", "wsl", "linux" };
            rawNames = rawNames.Where(n => !headerKeywords.Any(h => n.Equals(h, StringComparison.OrdinalIgnoreCase))).ToList();

            var map = ParseVerbose(verbose);

            _distros.Clear();
            foreach (var name in rawNames)
            {
                map.TryGetValue(name, out var detail);
                var installPath = Path.Combine(InstallRootTextBox.Text, SafeName(name));
                var latest = GetLatestBackup(name);

                string displayState;
                if (detail?.State != null)
                {
                    displayState = NormalizeState(detail.State);
                }
                else
                {
                    displayState = LocalizationManager.Unknown;
                }

                _distros.Add(new DistroInfo
                {
                    Name = name,
                    State = displayState,
                    Version = detail?.Version ?? 0,
                    InstallPath = installPath,
                    LatestBackupFile = latest?.FullName,
                    LatestBackupDisplay = latest == null
                        ? LocalizationManager.NoBackup
                        : $"{latest.Name} ({Math.Round(latest.Length / 1024d / 1024d, 2)} MB)"
                });
            }
            SetStatus(LocalizationManager.T("Loaded", _distros.Count));
        }
        catch (Exception ex)
        {
            SetStatus(LocalizationManager.T("LoadFailed", ex.Message));
        }
    }

    private static string NormalizeState(string state)
    {
        // Normalize state to localized display
        var s = state.Trim().ToLowerInvariant();
        if (s.Contains("running") || s.Contains("正在執行") || s.Contains("正在运行"))
            return LocalizationManager.Running;
        if (s.Contains("stopped") || s.Contains("已停止"))
            return LocalizationManager.Stopped;
        if (s.Contains("installing") || s.Contains("正在安裝") || s.Contains("正在安装"))
            return LocalizationManager.T("ColState"); // "Installing"
        if (s.Contains("uninstalling") || s.Contains("正在解除安裝") || s.Contains("正在卸载"))
            return LocalizationManager.T("ColState"); // "Uninstalling"
        return state;
    }

    private async Task BackupSelectedAsync()
    {
        if (DistroGrid.SelectedItem is not DistroInfo item)
        {
            SetStatus(LocalizationManager.SelectFirst);
            return;
        }

        Directory.CreateDirectory(Path.Combine(BackupRootTextBox.Text, SafeName(item.Name)));
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var useVhd = ((System.Windows.Controls.ComboBoxItem)BackupFormatComboBox.SelectedItem)?.Content?.ToString() == "vhdx";
        var ext = useVhd ? "vhdx" : "tar";
        var target = Path.Combine(BackupRootTextBox.Text, SafeName(item.Name), $"{SafeName(item.Name)}-{stamp}.{ext}");

        var confirm = MessageBox.Show(
            LocalizationManager.T("BackupConfirmMsg", item.Name, target),
            LocalizationManager.T("ConfirmBackup"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            SetStatus(LocalizationManager.T("Backupping", item.Name));
            await RunWslCaptureAsync("--shutdown");
            await RunWslCaptureAsync(useVhd ? $"--export \"{item.Name}\" \"{target}\" --vhd" : $"--export \"{item.Name}\" \"{target}\"");
            SetStatus(LocalizationManager.T("BackupSuccess", target));
            await LoadDistrosAsync();
        }
        catch (Exception ex)
        {
            SetStatus(LocalizationManager.T("BackupFailed", ex.Message));
        }
    }

    private async Task RestoreSelectedAsync()
    {
        if (DistroGrid.SelectedItem is not DistroInfo item)
        {
            SetStatus(LocalizationManager.SelectFirst);
            return;
        }

        var backup = GetLatestBackup(item.Name);
        if (backup == null)
        {
            SetStatus(LocalizationManager.NoBackupFound);
            return;
        }

        var installPath = Path.Combine(InstallRootTextBox.Text, SafeName(item.Name));
        var msg = LocalizationManager.T("RestoreConfirmMsg", item.Name, backup.FullName, installPath);
        var confirm = MessageBox.Show(msg, LocalizationManager.T("ConfirmRestore"), MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            SetStatus(LocalizationManager.T("Restoring", item.Name));
            await RunWslCaptureAsync("--shutdown");
            await RunWslCaptureAsync($"--unregister \"{item.Name}\"");

            if (Directory.Exists(installPath))
                Directory.Delete(installPath, true);
            Directory.CreateDirectory(installPath);

            var importArgs = backup.Extension.Equals(".vhdx", StringComparison.OrdinalIgnoreCase) || backup.Extension.Equals(".vhd", StringComparison.OrdinalIgnoreCase)
                ? $"--import \"{item.Name}\" \"{installPath}\" \"{backup.FullName}\" --vhd"
                : $"--import \"{item.Name}\" \"{installPath}\" \"{backup.FullName}\" --version 2";
            await RunWslCaptureAsync(importArgs);
            SetStatus(LocalizationManager.T("RestoreSuccess", item.Name));
            await LoadDistrosAsync();
        }
        catch (Exception ex)
        {
            SetStatus(LocalizationManager.T("RestoreFailed", ex.Message));
        }
    }

    private FileInfo? GetLatestBackup(string distroName)
    {
        var dir = Path.Combine(BackupRootTextBox.Text, SafeName(distroName));
        if (!Directory.Exists(dir)) return null;
        return new DirectoryInfo(dir)
            .GetFiles()
            .Where(f => new[] { ".tar", ".vhd", ".vhdx" }.Contains(f.Extension, StringComparer.OrdinalIgnoreCase))
            .OrderByDescending(f => f.LastWriteTime)
            .FirstOrDefault();
    }

    private static Dictionary<string, VerboseInfo> ParseVerbose(string text)
    {
        var map = new Dictionary<string, VerboseInfo>(StringComparer.OrdinalIgnoreCase);

        // Remove BOM
        var clean = text.Replace("\uFEFF", "");
        var lines = clean.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

        foreach (var raw in lines)
        {
            var line = raw.TrimStart('*', ' ', '\u200B' /* zero-width space */).Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            // Skip header lines (NAME header row or localized equivalents)
            if (Regex.IsMatch(line, @"^(name|名稱|NAME)\s+", RegexOptions.IgnoreCase)) continue;

            // Skip pure header separator lines like "------------------------------"
            if (Regex.IsMatch(line, @"^[-─═\u2550\u2500]{3,}$")) continue;

            // Try multiple patterns: "NAME   STATE   VERSION" (English)
            // and localized versions with various whitespace
            // The key insight: state is always a single word, version is the last number
            var m = Regex.Match(line, @"^(.+?)\s{2,}(\S+)\s+(\d+)\s*$");
            if (!m.Success) continue;

            var name = m.Groups[1].Value.Trim();
            var state = m.Groups[2].Value.Trim();
            if (int.TryParse(m.Groups[3].Value.Trim(), out var version))
            {
                map[name] = new VerboseInfo { State = state, Version = version };
            }
        }
        return map;
    }

    private static string SafeName(string name) => Regex.Replace(name, "[^A-Za-z0-9._\u4e00-\u9fff-]", "_");

    private async Task RunSimpleCommandAsync(string args, string okMessage)
    {
        try
        {
            await RunWslCaptureAsync(args);
            SetStatus(okMessage);
            await LoadDistrosAsync();
        }
        catch (Exception ex)
        {
            SetStatus(LocalizationManager.T("OperationFailed", ex.Message));
        }
    }

    private static async Task<string> RunWslCaptureAsync(string arguments)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "wsl.exe",
            Arguments = arguments,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        using var process = new Process { StartInfo = psi };
        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();
        if (process.ExitCode != 0)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr) ? $"WSL command failed: {arguments}" : stderr.Trim());
        return stdout;
    }

    private static void OpenFolder(string path)
    {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true
        });
    }

    private void SetStatus(string message) => StatusTextBlock.Text = message;
}

public class DistroInfo : INotifyPropertyChanged
{
    public string Name { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public int Version { get; set; }
    public string InstallPath { get; set; } = string.Empty;
    public string? LatestBackupFile { get; set; }
    public string LatestBackupDisplay { get; set; } = string.Empty;
    public event PropertyChangedEventHandler? PropertyChanged;
}

public class VerboseInfo
{
    public string State { get; set; } = string.Empty;
    public int Version { get; set; }
}
