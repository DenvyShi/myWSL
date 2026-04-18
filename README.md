# WSL Backup Manager

一個 WPF (Windows Presentation Foundation) 的 WSL 備份管理工具，透過圖形介面輕鬆管理 WSL distro 的備份與還原。

## 功能一覽

- **WSL 列表**：查看所有 distro 的名稱、狀態、版本
- **備份**：支援 `tar` 和 `vhdx` 兩種格式
- **還原**：一鍵還原到最新備份
- **停止全部**：一鍵停止所有運行中的 WSL
- **快速開啟**：直接打開備份目錄或安裝目錄

## 技術架構

| 項目 | 技術 |
|------|------|
| 框架 | WPF (.NET) |
| 語言 | C# |
| 後端調用 | `wsl.exe` CLI |
| UI 線程 | async/await 非阻塞 |

## 核心命令

```powershell
# 列出所有 WSL
wsl --list --verbose

# 導出備份（tar）
wsl --export <name> <file>

# 導出備份（vhdx）
wsl --export <name> <file> --vhd

# 導入還原
wsl --import <name> <installPath> <file>

# 停止全部 WSL
wsl --shutdown

# 刪除 distro
wsl --unregister <name>
```

## 程式碼品質分析

### ✅ 做得好的地方

- **async/await 非阻塞**：所有 IO 操作（`RunWslCaptureAsync`）都是 async，避免 UI 線程阻塞
- **操作確認對話框**：備份/還原前都有 `MessageBox.Show` 確認，防止誤操作
- **路徑安全處理**：`SafeName()` 用正則移除非法字元，避免路徑衝突
- **統一錯誤處理**：`RunWslCaptureAsync` 統一封裝，失敗時拋出 `InvalidOperationException`
- **即時狀態回饋**：`SetStatus()` 讓用戶清楚知道當前操作進度

### ⚠️ 潛在問題

1. **stderr 可能為空時錯誤訊息不清**：當 `wsl.exe` 失敗但 stderr 也是空，僅顯示 `"WSL command failed"`
2. **還原前未驗證備份檔存在**：`GetLatestBackup()` 可能回傳 null 或已被刪除的檔案
3. **大檔操作無進度條**：`export/import` 是同步等待，沒有 `ProgressBar`，大檔案時 UI 無響應
4. **路徑硬編碼**：預設 `D:\WSL-Backups` / `D:\WSL`，不適用於其他磁碟或非英文系統
5. **無日誌/歷史記錄**：無法查詢過往備份操作記錄
6. **還原時 `Directory.CreateDirectory` 路徑錯誤**：還原中 `Directory.CreateDirectory(InstallRootTextBox.Text)` 會在錯誤位置建立目錄，應該用 `installPath`

## 可改進方向

### 高優先
- 加入 **任務日誌面板**，記錄每次備份/還原的時間與結果
- 加入 **ProgressBar**，大檔操作時顯示進度
- 修復還原時 `Directory.CreateDirectory` 的路徑 bug

### 中優先
- 支援 **還原到任意歷史版本**（而不只是最新備份）
- 備份 **歷史列表**，用戶可選擇要還原哪個時間點的版本
- 路徑改為寫入 `appsettings.json` 而不是寫死

### 低優先（未來可擴展）
- **排程備份**：每日/每週自動備份
- **OpenClaw 健康檢查**：restore 後自動測試 WSL 能否啟動

## 編譯與發布

```powershell
# 開發模式運行
dotnet run

# 發布為單檔 EXE（self-contained，win-x64）
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## 專案結構

```
mywsl/
├── App.xaml              # WPF 應用程式 entry point
├── App.xaml.cs
├── MainWindow.xaml       # 視窗佈局（XAML）
├── MainWindow.xaml.cs    # 業務邏輯（C#）
├── WslBackupManager.csproj
├── build-release.bat     # 快速編譯腳本
└── README.md
```

## 許可證

本專案僅供個人使用與學習參考。
