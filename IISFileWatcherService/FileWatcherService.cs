using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Security.Policy;
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
        private string _statusLogFilePath;

        private HttpListener _listener;
        private readonly string _url = "http://localhost:5000/";

        public FileWatcherService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            StartHttpServer();

            _sourcePath = @"c:\TempFiles\from"; // Change this to the servers source path

            _logFilePath = Path.Combine(Directory.GetParent(_sourcePath)?.FullName ?? _sourcePath, "error.log");
            _statusLogFilePath = Path.Combine(Directory.GetParent(_sourcePath)?.FullName ?? _sourcePath, "status.log");

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
            File.AppendAllText(_statusLogFilePath, $"{DateTime.Now}: File copy successful.\n");
        }

        private string[] GetDestinationPaths()
        {
            string filePath = Path.Combine(_sourcePath, "startcopy.txt");

            try
            {
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"The file {filePath} was not found\n.");
                }
                string[] destinations = File.ReadAllText(filePath).Split(';');

                File.Delete(filePath);

                return destinations;
            }
            catch (Exception ex)
            {
                File.AppendAllText(_logFilePath, $"{DateTime.Now}: {ex.Message}\n");
                return new[] { "" };
            }
        }


        private bool CopyFileToDestinations()
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
                        const int copyRetries = 3;
                        var copyCount = 0;
                        Exception lastException = null;

                        do
                        {
                            try
                            {
                                File.Copy(sourceFile, destinationFile, true);

                                if (!CompareFileHashes(sourceFile, destinationFile))
                                {
                                    copyFilesErrorCount++;
                                    File.AppendAllText(_logFilePath, $"{DateTime.Now}: File hash mismatch detected for file: '{sourceFile}'\n");
                                    break;
                                }
                                bCopySuccess = true;
                            }
                            catch (Exception e)
                            {
                                lastException = e;

                                copyCount++;
                                copyFilesRetryCount++;
                                File.AppendAllText(_logFilePath, $"Failed to copy file from: '{sourceFile}' to: '{destinationFile}'. Errormessage: {e.Message}. Retry count: {copyFilesRetryCount}. Retrying...");

                                if (copyCount < copyRetries)
                                {
                                    Thread.Sleep(copyCount * 200);
                                }
                            }
                        } while (bCopySuccess == false && copyCount < copyRetries);

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
            return true;
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
            return new List<string>(Directory.GetFiles(_sourcePath, "*", SearchOption.AllDirectories));
        }


        protected override void OnStop()
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Dispose();

            if (_listener != null)
            {
                _listener.Stop();
                _listener.Close();
            }
        }


        private async void StartHttpServer()
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add(_url);
            _listener.Start();

            Task.Run(async () =>
            {
                while (_listener.IsListening)
                {
                    var context = await _listener.GetContextAsync();
                    ProcessRequest(context);
                }
            });
        }

        private void ProcessRequest(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;
            response.ContentType = "text/plain";

            if (request.HttpMethod == "GET")
            {
                string responseString = "Service is running!";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }
            else if (request.HttpMethod == "POST")
            {
                using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                {
                    string requestData = reader.ReadToEnd();
                    _destinationPaths = requestData.Split(';');
                    CopyFileToDestinations();

                    File.AppendAllText(_statusLogFilePath, $"{DateTime.Now}: Received POST: {requestData}\n");
                }

                string responseString = "POST request received!";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.OutputStream.Write(buffer, 0, buffer.Length);
            }

            response.Close();
        }
    }


}
