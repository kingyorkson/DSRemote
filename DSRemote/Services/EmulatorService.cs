using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using DSRemote.Models;

namespace DSRemote.Services;

public class EmulatorService
{
    private Process? _currentProcess;

    [DllImport("user32.dll")]
    private static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

    private const int SW_MINIMIZE = 6;
    private const int SW_HIDE = 0;
    private const int SW_SHOWNOACTIVATE = 4;
    private static readonly IntPtr HWND_BOTTOM = new(1);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOACTIVATE = 0x0010;

    public event Action<EmulatorState>? StateChanged;

    public EmulatorState CurrentState { get; private set; } = EmulatorState.Idle;

    public async Task<bool> LaunchGame(AppConfig config, GameRom game)
    {
        var emuPath = config.EmulatorPath;
        if (string.IsNullOrEmpty(emuPath) || !File.Exists(emuPath))
        {
            StateChanged?.Invoke(EmulatorState.Error);
            return false;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = emuPath,
            Arguments = $"\"{game.FullPath}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = Path.GetDirectoryName(emuPath)
        };

        try
        {
            _currentProcess = Process.Start(startInfo);
            if (_currentProcess == null)
            {
                StateChanged?.Invoke(EmulatorState.Error);
                return false;
            }

            // Wait for main window to appear, then minimize it
            for (int i = 0; i < 20; i++)
            {
                _currentProcess.Refresh();
                if (_currentProcess.MainWindowHandle != IntPtr.Zero)
                {
                    SetWindowPos(_currentProcess.MainWindowHandle, HWND_BOTTOM, 0, 0, 0, 0,
                        SWP_NOSIZE | SWP_NOACTIVATE);
                    ShowWindowAsync(_currentProcess.MainWindowHandle, SW_MINIMIZE);
                    break;
                }
                await Task.Delay(500);
            }

            CurrentState = EmulatorState.Running;
            StateChanged?.Invoke(EmulatorState.Running);

            await _currentProcess.WaitForExitAsync();

            CurrentState = EmulatorState.Idle;
            StateChanged?.Invoke(EmulatorState.Idle);
            _currentProcess = null;
            return true;
        }
        catch
        {
            StateChanged?.Invoke(EmulatorState.Error);
            return false;
        }
    }

    public void StopEmulation()
    {
        if (_currentProcess != null && !_currentProcess.HasExited)
        {
            _currentProcess.Kill();
            _currentProcess.Dispose();
            _currentProcess = null;
        }
        CurrentState = EmulatorState.Idle;
        StateChanged?.Invoke(EmulatorState.Idle);
    }

    public IntPtr? GetEmulatorWindowHandle()
    {
        if (_currentProcess == null || _currentProcess.HasExited)
            return null;

        _currentProcess.Refresh();
        return _currentProcess.MainWindowHandle != IntPtr.Zero
            ? _currentProcess.MainWindowHandle
            : null;
    }

    public bool RestoreWindowForCapture()
    {
        var hWnd = GetEmulatorWindowHandle();
        if (hWnd == null || hWnd.Value == IntPtr.Zero) return false;
        ShowWindowAsync(hWnd.Value, SW_SHOWNOACTIVATE);
        return true;
    }

    public void ReMinimizeWindow()
    {
        var hWnd = GetEmulatorWindowHandle();
        if (hWnd == null || hWnd.Value == IntPtr.Zero) return;
        ShowWindowAsync(hWnd.Value, SW_MINIMIZE);
    }
}

public enum EmulatorState
{
    Idle,
    Running,
    Error
}
