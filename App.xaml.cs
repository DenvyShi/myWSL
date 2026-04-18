using System;
using System.IO;
using System.Windows;
using System.Windows.Threading;

namespace WslBackupManager;

public partial class App : Application
{
    public App()
    {
        InitializeComponent();
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            File.WriteAllText(@"C:\WslBackupManager_error.log",
                $"[{DateTime.Now}] FATAL: {e.Exception.Message}\n{e.Exception.StackTrace}");
        }
        catch { }
        e.Handled = true;
        Environment.Exit(1);
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        try
        {
            var ex = e.ExceptionObject as Exception;
            File.WriteAllText(@"C:\WslBackupManager_error.log",
                $"[{DateTime.Now}] FATAL: {ex?.Message}\n{ex?.StackTrace}");
        }
        catch { }
        Environment.Exit(1);
    }
}
