using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
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
        private string _logInfo;
        private HttpListener _listener;
        private readonly string _url = "http://localhost:5000/";

        public FileWatcherService()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            _sourcePath = @"c:\TempFiles\from"; // Change this to the servers source path
            
            _logInfo = $"{DateTime.Now}: Service running";

            if (!Directory.Exists(_sourcePath))
            {
                Directory.CreateDirectory(_sourcePath);
            }
            StartHttpServer();
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
                string responseString = _logInfo;
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
                }

                string responseString = _logInfo;
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                response.OutputStream.Write(buffer, 0, buffer.Length);
                _logInfo = $"{DateTime.Now}: Service running";
            }
            response.Close();
        }


        private void CopyFileToDestinations()
        {
            var copyFilesCount = 1;
            var copyFilesErrorCount = 0;
            var copyFilesRetryCount = 0;

            foreach (string destinationPath in _destinationPaths)
            {
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
                                    _logInfo = $"{DateTime.Now}: File hash mismatch detected for file: '{sourceFile}'";
                                    break;
                                }
                                bCopySuccess = true;
                            }
                            catch (Exception e)
                            {
                                lastException = e;

                                copyCount++;
                                copyFilesRetryCount++;
                                _logInfo = $"Failed to copy file from: '{sourceFile}' to: '{destinationFile}'. Errormessage: {e.Message}. Retry count: {copyFilesRetryCount}. Retrying...";

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
                            _logInfo = $"Failed to copy file from: '{sourceFile}' to: '{destinationFile}'. All retries failed.";
                        }
                    }
                    catch (Exception e)
                    {
                        _logInfo = $"\t{_sourcePath}: Error copying file to: '{destinationFile}'";
                        copyFilesErrorCount++;
                    }

                    copyFilesCount++;
                }
            }
            if (copyFilesErrorCount == 0)
                _logInfo = $"{DateTime.Now}: Copying files completed successfully";
            else
                _logInfo = $"{DateTime.Now}: Copying files completed with errors";
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
                _logInfo = $"{DateTime.Now}: Hash mismatch for file '{sourceFile}' and destination '{destinationFile}'\n";
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
            if (_listener != null)
            {
                _listener.Stop();
                _listener.Close();
            }
        }
    }

}
