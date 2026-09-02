using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deepseek
{
    public class PeriodicFolderScanner
    {
        private readonly string _folderPath;
        private readonly Func<string, Task> _fileProcessor;
        private readonly Dictionary<string, DateTime> _processedFiles = new();
        private readonly int _intervalMs;
        private CancellationTokenSource _cts;

        public PeriodicFolderScanner(string folderPath, Func<string, Task> fileProcessor, int intervalMs = 2000)
        {
            _folderPath = folderPath;
            _fileProcessor = fileProcessor;
            _intervalMs = intervalMs;
        }

        public void Start()
        {
            _cts = new CancellationTokenSource();
            Task.Run(() => ScanLoopAsync(_cts.Token));
        }

        public void Stop()
        {
            _cts?.Cancel();
        }

        private async Task ScanLoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (Directory.Exists(_folderPath))
                    {
                        var files = Directory.GetFiles(_folderPath);
                        foreach (var file in files)
                        {
                            var lastWrite = File.GetLastWriteTimeUtc(file);
                            if (_processedFiles.TryGetValue(file, out var prevWrite) && prevWrite == lastWrite)
                                continue;
                            _processedFiles[file] = lastWrite;

                            try
                            {
                                await _fileProcessor(file);
                            }
                            catch (Exception ex)
                            {
                                // Если обработка не удалась, можно убрать из processed, чтобы попробовать снова
                                _processedFiles.Remove(file);
                                // Логирование
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Логирование
                }

                await Task.Delay(_intervalMs, token);
            }
        }
    }
}
