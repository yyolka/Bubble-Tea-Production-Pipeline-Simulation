using System;

namespace BubbleTea.Core
{
    public interface ILogger
    {
        void Log(string message);
        void LogError(string message);
    }

    public class FileLogger : ILogger, IDisposable
    {
        private readonly System.IO.StreamWriter _writer;
        private readonly bool _enableConsole;
        private readonly object _lock = new();

        public FileLogger(string filePath, bool enableConsole = true)
        {
            _enableConsole = enableConsole;
            
            var directory = System.IO.Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            _writer = new System.IO.StreamWriter(filePath, append: false);
        }

        public void Log(string message)
        {
            string logMessage = $"{DateTime.Now:HH:mm:ss} - {message}";
            
            lock (_lock)
            {
                if (_enableConsole)
                {
                    Console.WriteLine(logMessage);
                }
                _writer.WriteLine(logMessage);
            }
        }

        public void LogError(string message)
        {
            Log($"ERROR: {message}");
        }

        public void Dispose()
        {
            _writer?.Flush();
            _writer?.Dispose();
        }
    }
}