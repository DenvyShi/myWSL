using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace WslBackupManager;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<DistroInfo> _distros = new();

    public MainWindow()
    {
        InitializeComponent();
        DistroGrid.ItemsSource = _distros;

        RefreshButton.Click += async (_, _) => await LoadDistrosAsync();
        ShutdownAllButton.Click += async (_, _) => await RunSimpleCommandAsync("--shutdown", "所有 WSL 已停止。");
        BackupButton.Click += async (_, _) => await BackupSelectedAsync();
        RestoreButton.Click += async (_, _) => await RestoreSelectedAsync();
        OpenBackupFolderButton.Click += (_, _) => OpenFolder(BackupRootTextBox.Text);
        OpenInstallFolderButton.Click += (_, _) => OpenFolder(InstallRootTextBox.Text);
        DistroGrid.SelectionChanged += (_, _) => SyncSelection();

        Loaded += async (_, _) => await LoadDistrosAsync();
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
            SetStatus("正在讀取 WSL 列表...");
            var basic = await RunWslCaptureAsync("-l -q");
            var verbose = await RunWslCaptureAsync("-l -v");

            var names = basic.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(x => x.Trim())
                             .Where(x => !string.IsNullOrWhiteSpace(x))
                             .ToList();
            var map = ParseVerbose(verbose);

            _distros.Clear();
            foreach (var name in names)
            {
                map.TryGetValue(name, out var detail);
                var installPath = Path.Combine(InstallRootTextBox.Text, SafeName(name));
                var latest = GetLatestBackup(name);
                _distros.Add(new DistroInfo
                {
                    Name = name,
                    State = detail?.State ?? "Unknown",
                    Version = detail?.Version ?? 0,
                    InstallPath = installPath,
                    LatestBackupFile = latest?.FullName,
                    LatestBackupDisplay = latest == null ? "尚無備份" : $"{latest.Name} ({Math.Round(latest.Length / 1024d / 1024d, 2)} MB)"
                });
            }
            SetStatus($"已載入 {_distros.Count} 個 WSL 實例。");
        }
        catch (Exception ex)
        {
            SetStatus($"讀取失敗：{ex.Message}");
        }
    }

    private async Task BackupSelectedAsync()
    {
        if (DistroGrid.SelectedItem is not DistroInfo item)
        {
            SetStatus("請先選擇一個 WSL。");
            return;
        }

        Directory.CreateDirectory(Path.Combine(BackupRootTextBox.Text, SafeName(item.Name)));
        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var useVhd = ((System.Windows.Controls.ComboBoxItem)BackupFormatComboBox.SelectedItem)?.Content?.ToString() == "vhdx";
        var ext = useVhd ? "vhdx" : "tar";
        var target = Path.Combine(BackupRootTextBox.Text, SafeName(item.Name), $"{SafeName(item.Name)}-{stamp}.{ext}");

        var confirm = MessageBox.Show($"確定備份 {item.Name} 到\n{target} ?", "確認備份", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            SetStatus($"正在備份 {item.Name} ...");
            await RunWslCaptureAsync("--shutdown");
            await RunWslCaptureAsync(useVhd ? $"--export \"{item.Name}\" \"{target}\" --vhd" : $"--export \"{item.Name}\" \"{target}\"");
            SetStatus($"備份完成：{target}");
            await LoadDistrosAsync();
        }
        catch (Exception ex)
        {
            SetStatus($"備份失敗：{ex.Message}");
        }
    }

    private async Task RestoreSelectedAsync()
    {
        if (DistroGrid.SelectedItem is not DistroInfo item)
        {
            SetStatus("請先選擇一個 WSL。");
            return;
        }

        var backup = GetLatestBackup(item.Name);
        if (backup == null)
        {
            SetStatus("找不到最新備份，無法還原。");
            return;
        }

        var installPath = Path.Combine(InstallRootTextBox.Text, SafeName(item.Name));
        var msg = $"即將還原 {item.Name}\n\n來源：{backup.FullName}\n目標：{installPath}\n\n這會先 unregister 現有同名 WSL。";
        var confirm = MessageBox.Show(msg, "確認還原", MessageBoxButton.YesNo, MessageBoxImage.Warning);
        if (confirm != MessageBoxResult.Yes) return;

        try
        {
            SetStatus($"正在還原 {item.Name} ...");
            await RunWslCaptureAsync("--shutdown");
            await RunWslCaptureAsync($"--unregister \"{item.Name}\"");

            if (Directory.Exists(installPath))
                Directory.Delete(installPath, true);
            Directory.CreateDirectory(InstallRootTextBox.Text);

            var importArgs = backup.Extension.Equals(".vhdx", StringComparison.OrdinalIgnoreCase) || backup.Extension.Equals(".vhd", StringComparison.OrdinalIgnoreCase)
                ? $"--import \"{item.Name}\" \"{installPath}\" \"{backup.FullName}\" --vhd"
                : $"--import \"{item.Name}\" \"{installPath}\" \"{backup.FullName}\" --version 2";
            await RunWslCaptureAsync(importArgs);
            SetStatus($"還原完成：{item.Name}");
            await LoadDistrosAsync();
        }
        catch (Exception ex)
        {
            SetStatus($"還原失敗：{ex.Message}");
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
        foreach (var raw in text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var line = raw.TrimStart('*', ' ').Trim();
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("NAME", StringComparison.OrdinalIgnoreCase))
                continue;
            var m = Regex.Match(line, "^(.*?)\\s{2,}(Running|Stopped|Installing|Uninstalling)\\s+(\\d+)$");
            if (!m.Success) continue;
            map[m.Groups[1].Value.Trim()] = new VerboseInfo
            {
                State = m.Groups[2].Value.Trim(),
                Version = int.Parse(m.Groups[3].Value.Trim())
            };
        }
        return map;
    }

    private static string SafeName(string name) => Regex.Replace(name, "[^A-Za-z0-9._-]", "_");

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
            SetStatus($"操作失敗：{ex.Message}");
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
            CreateNoWindow = true
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
