namespace LitTemplate;

public enum LogLevel {
    Info,
    Warning,
    Error,
    None
}

public static class Log {
    public static Action<LogLevel, string> ActionOnLog;
    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Global
    public static LogLevel Level { get; set; } = LogLevel.Info;

    static Log() =>
        ActionOnLog = (level, message) => {
            if (level < Level) {
                return;
            }
            Console.WriteLine($"[{level}] {message}");
        };

    public static void Info(string message) => ActionOnLog?.Invoke(LogLevel.Info, message);

    public static void Warning(string message) => ActionOnLog?.Invoke(LogLevel.Warning, message);
    
    public static void Error(string message) => ActionOnLog?.Invoke(LogLevel.Error, message);
}
