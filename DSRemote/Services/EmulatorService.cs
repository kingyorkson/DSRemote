using System.Diagnostics;
using System.IO;
using DSRemote.Models;

namespace DSRemote.Services;

public class EmulatorService
{
    private Process? _currentProcess;

    public event Action<EmulatorState>? StateChanged;

    public EmulatorState CurrentState { get; private set; } = EmulatorState.Idle;

    public async Task<bool> LaunchGame(AppConfig config, GameRom game)
    {
        if (!File.Exists(config.EmulatorPath))
        {
            StateChanged?.Invoke(EmulatorState.Error);
            return false;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = config.EmulatorPath,
            Arguments = $"\"{game.FullPath}\"",
            UseShellExecute = true,
            WorkingDirectory = Path.GetDirectoryName(config.EmulatorPath)
        };

        try
        {
            _currentProcess = Process.Start(startInfo);
            if (_currentProcess == null)
            {
                StateChanged?.Invoke(EmulatorState.Error);
                return false;
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
}

public enum EmulatorState
{
    Idle,
    Running,
    Error
}
