using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WslBackupManager;

public static class LocalizationManager
{
    public static event PropertyChangedEventHandler? StaticPropertyChanged;

    private static void NotifyStatic([CallerMemberName] string? name = null)
        => StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(name));

    public enum Language { English, TraditionalChinese, SimplifiedChinese }

    private static Language _currentLang = Language.TraditionalChinese;
    public static Language CurrentLang
    {
        get => _currentLang;
        set { _currentLang = value; NotifyStatic(); }
    }

    private static readonly Dictionary<string, Dictionary<Language, string>> _dict = new()
    {
        // Window title
        ["AppTitle"]         = new() { { Language.English, "WSL Backup Manager" }, { Language.TraditionalChinese, "WSL 備份管理工具" }, { Language.SimplifiedChinese, "WSL 备份管理工具" } },
        ["AppSubtitle"]      = new() { { Language.English, "View WSL instances, backup and restore with ease." }, { Language.TraditionalChinese, "可視化查看 WSL 名稱、狀態、版本與最新備份，並直接在桌面應用中執行備份與還原。" }, { Language.SimplifiedChinese, "可视化查看 WSL 名称、状态、版本与最新备份，并直接在桌面应用中执行备份与还原。" } },

        // Header buttons
        ["Refresh"]          = new() { { Language.English, "Refresh" }, { Language.TraditionalChinese, "刷新列表" }, { Language.SimplifiedChinese, "刷新列表" } },
        ["ShutdownAll"]       = new() { { Language.English, "Stop All WSL" }, { Language.TraditionalChinese, "停止全部WSL" }, { Language.SimplifiedChinese, "停止全部WSL" } },

        // Left panel
        ["Config"]           = new() { { Language.English, "Settings" }, { Language.TraditionalChinese, "設定" }, { Language.SimplifiedChinese, "设置" } },
        ["ConfigDesc"]       = new() { { Language.English, "Configure backup directory, install directory and default backup format." }, { Language.TraditionalChinese, "設定備份目錄、安裝目錄與預設備份格式。" }, { Language.SimplifiedChinese, "设置备份目录、安装目录与默认备份格式。" } },
        ["BackupRoot"]        = new() { { Language.English, "Backup Root" }, { Language.TraditionalChinese, "備份根目錄" }, { Language.SimplifiedChinese, "备份根目录" } },
        ["InstallRoot"]       = new() { { Language.English, "Install Root" }, { Language.TraditionalChinese, "安裝根目錄" }, { Language.SimplifiedChinese, "安装根目录" } },
        ["BackupFormat"]      = new() { { Language.English, "Default Format" }, { Language.TraditionalChinese, "預設備份格式" }, { Language.SimplifiedChinese, "默认备份格式" } },
        ["SelectedDistro"]    = new() { { Language.English, "Selected WSL" }, { Language.TraditionalChinese, "當前選中WSL" }, { Language.SimplifiedChinese, "当前选中WSL" } },
        ["BackupNow"]         = new() { { Language.English, "Backup Now" }, { Language.TraditionalChinese, "立即備份" }, { Language.SimplifiedChinese, "立即备份" } },
        ["Restore"]           = new() { { Language.English, "Restore Latest" }, { Language.TraditionalChinese, "還原最新備份" }, { Language.SimplifiedChinese, "还原最新备份" } },
        ["OpenBackupFolder"]  = new() { { Language.English, "Open Backup Dir" }, { Language.TraditionalChinese, "打開備份目錄" }, { Language.SimplifiedChinese, "打开备份目录" } },
        ["OpenInstallFolder"] = new() { { Language.English, "Open Install Dir" }, { Language.TraditionalChinese, "打開安裝目錄" }, { Language.SimplifiedChinese, "打开安装目录" } },

        // Right panel
        ["WslInstances"]      = new() { { Language.English, "WSL Instances" }, { Language.TraditionalChinese, "WSL 實例" }, { Language.SimplifiedChinese, "WSL 实例" } },
        ["WslInstancesDesc"] = new() { { Language.English, "Select a distro to backup or restore." }, { Language.TraditionalChinese, "點選某個 distro 後，可直接執行備份與還原。" }, { Language.SimplifiedChinese, "点选某个 distro 后，可直接执行备份与还原。" } },
        ["ColName"]           = new() { { Language.English, "Name" }, { Language.TraditionalChinese, "名稱" }, { Language.SimplifiedChinese, "名称" } },
        ["ColState"]          = new() { { Language.English, "State" }, { Language.TraditionalChinese, "狀態" }, { Language.SimplifiedChinese, "状态" } },
        ["ColVersion"]        = new() { { Language.English, "Version" }, { Language.TraditionalChinese, "版本" }, { Language.SimplifiedChinese, "版本" } },
        ["ColLatestBackup"]   = new() { { Language.English, "Latest Backup" }, { Language.TraditionalChinese, "最新備份" }, { Language.SimplifiedChinese, "最新备份" } },
        ["ColInstallPath"]    = new() { { Language.English, "Install Path" }, { Language.TraditionalChinese, "安裝路徑" }, { Language.SimplifiedChinese, "安装路径" } },

        // Status (ready)
        ["Ready"]             = new() { { Language.English, "Ready." }, { Language.TraditionalChinese, "就緒。" }, { Language.SimplifiedChinese, "就绪。" } },

        // States
        ["Running"]           = new() { { Language.English, "Running" }, { Language.TraditionalChinese, "執行中" }, { Language.SimplifiedChinese, "运行中" } },
        ["Stopped"]           = new() { { Language.English, "Stopped" }, { Language.TraditionalChinese, "已停止" }, { Language.SimplifiedChinese, "已停止" } },
        ["Unknown"]           = new() { { Language.English, "Unknown" }, { Language.TraditionalChinese, "未知" }, { Language.SimplifiedChinese, "未知" } },

        // Backup display
        ["NoBackup"]          = new() { { Language.English, "No backup" }, { Language.TraditionalChinese, "尚無備份" }, { Language.SimplifiedChinese, "尚无备份" } },

        // Status messages
        ["Loading"]           = new() { { Language.English, "Loading WSL list..." }, { Language.TraditionalChinese, "正在讀取 WSL 列表..." }, { Language.SimplifiedChinese, "正在读取 WSL 列表..." } },
        ["Loaded"]            = new() { { Language.English, "Loaded {0} WSL instance(s)." }, { Language.TraditionalChinese, "已載入 {0} 個 WSL 實例。" }, { Language.SimplifiedChinese, "已载入 {0} 个 WSL 实例。" } },
        ["ShutdownOk"]        = new() { { Language.English, "All WSL stopped." }, { Language.TraditionalChinese, "所有 WSL 已停止。" }, { Language.SimplifiedChinese, "所有 WSL 已停止。" } },
        ["BackupSuccess"]     = new() { { Language.English, "Backup complete: {0}" }, { Language.TraditionalChinese, "備份完成：{0}" }, { Language.SimplifiedChinese, "备份完成：{0}" } },
        ["BackupFailed"]      = new() { { Language.English, "Backup failed: {0}" }, { Language.TraditionalChinese, "備份失敗：{0}" }, { Language.SimplifiedChinese, "备份失败：{0}" } },
        ["RestoreSuccess"]    = new() { { Language.English, "Restore complete: {0}" }, { Language.TraditionalChinese, "還原完成：{0}" }, { Language.SimplifiedChinese, "还原完成：{0}" } },
        ["RestoreFailed"]     = new() { { Language.English, "Restore failed: {0}" }, { Language.TraditionalChinese, "還原失敗：{0}" }, { Language.SimplifiedChinese, "还原失败：{0}" } },
        ["LoadFailed"]        = new() { { Language.English, "Load failed: {0}" }, { Language.TraditionalChinese, "讀取失敗：{0}" }, { Language.SimplifiedChinese, "读取失败：{0}" } },
        ["SelectFirst"]       = new() { { Language.English, "Please select a WSL first." }, { Language.TraditionalChinese, "請先選擇一個 WSL。" }, { Language.SimplifiedChinese, "请先选择一个 WSL。" } },
        ["NoBackupFound"]     = new() { { Language.English, "No backup found, cannot restore." }, { Language.TraditionalChinese, "找不到最新備份，無法還原。" }, { Language.SimplifiedChinese, "找不到最新备份，无法还原。" } },
        ["OperationFailed"]   = new() { { Language.English, "Operation failed: {0}" }, { Language.TraditionalChinese, "操作失敗：{0}" }, { Language.SimplifiedChinese, "操作失败：{0}" } },
        ["Backupping"]       = new() { { Language.English, "Backing up {0}..." }, { Language.TraditionalChinese, "正在備份 {0} ..." }, { Language.SimplifiedChinese, "正在备份 {0} ..." } },
        ["Restoring"]        = new() { { Language.English, "Restoring {0}..." }, { Language.TraditionalChinese, "正在還原 {0} ..." }, { Language.SimplifiedChinese, "正在还原 {0} ..." } },

        // Dialogs
        ["ConfirmBackup"]     = new() { { Language.English, "Confirm Backup" }, { Language.TraditionalChinese, "確認備份" }, { Language.SimplifiedChinese, "确认备份" } },
        ["ConfirmRestore"]    = new() { { Language.English, "Confirm Restore" }, { Language.TraditionalChinese, "確認還原" }, { Language.SimplifiedChinese, "确认还原" } },
        ["BackupConfirmMsg"]  = new() { { Language.English, "Backup {0} to\n{1}?" }, { Language.TraditionalChinese, "確定備份 {0} 到\n{1} ?" }, { Language.SimplifiedChinese, "确定备份 {0} 到\n{1} ?" } },
        ["RestoreConfirmMsg"] = new() { { Language.English, "About to restore {0}\n\nSource: {1}\nTarget: {2}\n\nThis will unregister existing WSL with the same name." }, { Language.TraditionalChinese, "即將還原 {0}\n\n來源：{1}\n目標：{2}\n\n這會先 unregister 現有同名 WSL。" }, { Language.SimplifiedChinese, "即将还原 {0}\n\n来源：{1}\n目标：{2}\n\n这会先 unregister 现有同名 WSL。" } },

        // Language names
        ["LangEN"]  = new() { { Language.English, "EN" }, { Language.TraditionalChinese, "EN" }, { Language.SimplifiedChinese, "EN" } },
        ["LangTC"]  = new() { { Language.English, "繁" }, { Language.TraditionalChinese, "繁" }, { Language.SimplifiedChinese, "繁" } },
        ["LangSC"]  = new() { { Language.English, "簡" }, { Language.TraditionalChinese, "簡" }, { Language.SimplifiedChinese, "简" } },
    };

    public static string T(string key)
    {
        if (_dict.TryGetValue(key, out var langs))
            if (langs.TryGetValue(_currentLang, out var val))
                return val;
        return key;
    }

    public static string T(string key, params object[] args)
    {
        return string.Format(T(key), args);
    }

    // Convenience accessors
    public static string AppTitle     => T("AppTitle");
    public static string AppSubtitle  => T("AppSubtitle");
    public static string Refresh      => T("Refresh");
    public static string ShutdownAll   => T("ShutdownAll");
    public static string Config       => T("Config");
    public static string ConfigDesc   => T("ConfigDesc");
    public static string BackupRoot   => T("BackupRoot");
    public static string InstallRoot  => T("InstallRoot");
    public static string BackupFormat => T("BackupFormat");
    public static string SelectedDistro => T("SelectedDistro");
    public static string BackupNow    => T("BackupNow");
    public static string Restore      => T("Restore");
    public static string OpenBackupFolder  => T("OpenBackupFolder");
    public static string OpenInstallFolder => T("OpenInstallFolder");
    public static string WslInstances => T("WslInstances");
    public static string WslInstancesDesc => T("WslInstancesDesc");
    public static string ColName      => T("ColName");
    public static string ColState     => T("ColState");
    public static string ColVersion   => T("ColVersion");
    public static string ColLatestBackup => T("ColLatestBackup");
    public static string ColInstallPath => T("ColInstallPath");
    public static string Ready        => T("Ready");
    public static string NoBackup     => T("NoBackup");
    public static string Loading      => T("Loading");
    public static string ShutdownOk   => T("ShutdownOk");
    public static string Running       => T("Running");
    public static string Stopped       => T("Stopped");
    public static string Unknown      => T("Unknown");
}
