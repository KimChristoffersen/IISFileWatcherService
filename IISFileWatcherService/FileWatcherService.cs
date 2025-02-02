using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.ServiceProcess;
using System.Threading;
using System.Threading.Tasks;

namespace IISFileWatcherService
{
    public partial class FileWatcherService : ServiceBase
    {
        private string _sourcePath;
        private string[] _destinationPaths;
        private FileSystemWatcher _watcher;
        private string _logFilePath;


        public FileWatcherService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _sourcePath = @"c:\TempFiles\from";
            _logFilePath = Path.Combine(Directory.GetParent(_sourcePath)?.FullName ?? _sourcePath, "status.log");

            if (!Directory.Exists(_sourcePath))
            {
                Directory.CreateDirectory(_sourcePath);
            }

            _watcher = new FileSystemWatcher(_sourcePath)
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
                Filter = "startcopy.*",
                EnableRaisingEvents = true
            };

            _watcher.Created += OnNewFileDetected;
        }

        private async void OnNewFileDetected(object sender, FileSystemEventArgs e)
        {
            await Task.Delay(1000);
            _destinationPaths = GetDestinationPaths();
            CopyFileToDestinations();
            File.AppendAllText(_logFilePath, $"{DateTime.Now}: File copy successful.\n");
        }

        private string[] GetDestinationPaths()
        {
            string filePath = Path.Combine(_sourcePath, "startcopy.txt");

            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Filen {filePath} blev ikke fundet\n.");
                }
                string[] destinations = File.ReadAllText(filePath).Split(';');

                File.Delete(filePath);

                return destinations;
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFilePath, $"{DateTime.Now}: {ex.Message}\n");
                return new string[] { "" };
            }
        }


        private void CopyFileToDestinations()
        {
            foreach (string destinationPath in _destinationPaths)
            {
                var copyFilesCount = 1;
                var copyFilesErrorCount = 0;
                var copyFilesRetryCount = 0;
                var sourceFileList = GetSourceFileList();

                foreach (string sourceFile in sourceFileList)
                {
                    var destinationFile = sourceFile.Replace(_sourcePath, destinationPath);
                    try
                    {
                        var currentFileDestinationDirectory = Path.GetDirectoryName(destinationFile);
                        if (!Directory.Exists(currentFileDestinationDirectory))
                        {
                            Directory.CreateDirectory(currentFileDestinationDirectory);
                        }

                        var bCopySuccess = false;
                        const int CopyRetries = 3;
                        var copyCount = 0;
                        Exception lastException = null;

                        do
                        {
                            try
                            {
                                File.Copy(sourceFile, destinationFile, true);
                                bCopySuccess = true;

                                if (!CompareFileHashes(sourceFile, destinationFile))
                                {
                                    copyFilesErrorCount++;
                                    File.AppendAllText(_logFilePath, $"{DateTime.Now}: File hash mismatch detected for file: '{sourceFile}'\n");
                                    break;
                                }

                                File.AppendAllText(_logFilePath, $"{DateTime.Now}: destinationfile {destinationFile} hash match sourcefile {sourceFile}'\n");
                            }
                            catch (Exception e)
                            {
                                lastException = e;

                                copyCount++;
                                copyFilesRetryCount++;
                                File.AppendAllText(_logFilePath, $"Failed to copy file from: '{sourceFile}' to: '{destinationFile}'. Errormessage: {e.Message}. Retry count: {copyFilesRetryCount}. Retrying...");

                                if (copyCount < CopyRetries)
                                {
                                    Thread.Sleep(copyCount * 200);
                                }
                            }
                        } while (bCopySuccess == false && copyCount < CopyRetries);

                        if (bCopySuccess == false)
                        {
                            copyFilesErrorCount++;
                            copyFilesRetryCount--;
                            File.AppendAllText(_logFilePath, $"Failed to copy file from: '{sourceFile}' to: '{destinationFile}'. All retries failed.");
                        }
                    }
                    catch (Exception e)
                    {
                        File.AppendAllText(_logFilePath, $"\t{_sourcePath}: Error copying file to: '{destinationFile}'");
                        copyFilesErrorCount++;
                    }

                    copyFilesCount++;
                }
            }
        }

        private string CalculateFileHash(string filePath)
        {
            using (var sha256 = SHA256.Create())
            {
                using (var fileStream = File.OpenRead(filePath))
                {
                    byte[] hashBytes = sha256.ComputeHash(fileStream);
                    return BitConverter.ToString(hashBytes).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        private bool CompareFileHashes(string sourceFile, string destinationFile)
        {
            string sourceHash = CalculateFileHash(sourceFile);
            string destinationHash = CalculateFileHash(destinationFile);

            if (sourceHash != destinationHash)
            {
                File.AppendAllText(_logFilePath, $"{DateTime.Now}: Hash mismatch for file '{sourceFile}' and destination '{destinationFile}'\n");
                return false;
            }

            return true;
        }


        private List<string> GetSourceFileList()
        {
            return new List<string>(System.IO.Directory.GetFiles(_sourcePath, "*", SearchOption.AllDirectories));
        }



        protected override void OnStop()
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();
        }
    }
}
