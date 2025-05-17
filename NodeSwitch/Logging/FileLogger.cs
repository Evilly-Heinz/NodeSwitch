using System;
using System.IO;
using System.Text;

namespace NodeSwitch.Logging
{
    public class FileLogger : ILogger
    {
        private readonly string _baseLogFilePath;
        private string _logFilePath;
        private DateTime _currentLogDate;
        private readonly object _lock = new();

        public FileLogger(string logFilePath)
        {
            // Remove extension if present, we'll add it after the date
            var ext = Path.GetExtension(logFilePath);
            var basePath = ext.Length > 0 ? logFilePath.Substring(0, logFilePath.Length - ext.Length) : logFilePath;
            _baseLogFilePath = basePath;
            _currentLogDate = DateTime.Today;
            _logFilePath = GetLogFilePathForDate(_currentLogDate);
            Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath)!);
        }

        private string GetLogFilePathForDate(DateTime date)
        {
            // Format: baseName_DDMMYYYY.log
            string dir = Path.GetDirectoryName(_baseLogFilePath)!;
            string file = Path.GetFileNameWithoutExtension(_baseLogFilePath);
            string ext = ".log";
            string dateStr = date.ToString("ddMMyyyy");
            return Path.Combine(dir, $"{file}_{dateStr}{ext}");
        }

        private void WriteLog(string severity, string classAndMethod, string message)
        {
            var now = DateTime.Now;
            lock (_lock)
            {
                // Check if the date has changed
                if (_currentLogDate != now.Date)
                {
                    _currentLogDate = now.Date;
                    _logFilePath = GetLogFilePathForDate(_currentLogDate);
                    Directory.CreateDirectory(Path.GetDirectoryName(_logFilePath)!);
                }
                var logLine = $"{now:yyyy-MM-dd HH:mm:ss} | {severity} | {classAndMethod} | {message}";
                File.AppendAllText(_logFilePath, logLine + Environment.NewLine, Encoding.UTF8);
            }
        }

        public void LogInformation(string classAndMethod, string message)
            => WriteLog("INFO", classAndMethod, message);

        public void LogWarning(string classAndMethod, string message)
            => WriteLog("WARN", classAndMethod, message);

        public void LogError(string classAndMethod, string message)
            => WriteLog("ERROR", classAndMethod, message);

        public void LogError(string classAndMethod, string message, Exception ex)
            => WriteLog("ERROR", classAndMethod, $"{message} | Exception: {ex}");
    }
}
