using System.Text;

namespace Engine.Diagnostics;

public enum LogLevel { Debug, Info, Warn, Error }

public sealed class EngineLogger : IDisposable
{
    private readonly StreamWriter _writer;
    private readonly bool _debugEnabled;

    public EngineLogger(string appName, string version, bool debugEnabled = false)
    {
        _debugEnabled = debugEnabled;
        Directory.CreateDirectory("logs");
        var path = Path.Combine("logs", $"{DateTime.Now:yyyyMMdd_HHmmss}.log");
        _writer = new StreamWriter(path, false, Encoding.UTF8) { AutoFlush = true };
        Info($"{appName} v{version} starting");
    }

    public void Log(LogLevel level, string msg)
    {
        if (level == LogLevel.Debug && !_debugEnabled)
            return;

        var line = $"{DateTime.Now:O} [{level}] {msg}";
        Console.WriteLine(line);
        _writer.WriteLine(line);
    }

    public void Debug(string msg) => Log(LogLevel.Debug, msg);
    public void Warn(string msg) => Log(LogLevel.Warn, msg);
    public void Error(string msg, Exception ex) => Log(LogLevel.Error, $"{msg}: {ex}");
    public void Info(string msg) => Log(LogLevel.Info, msg);
    public void Dispose() => _writer.Dispose();
}
