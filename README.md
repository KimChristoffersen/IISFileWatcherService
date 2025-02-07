<h2>📂 IIS File Watcher Service</h2>

<p>The <strong>IIS File Watcher Service</strong> is a Windows Service designed to monitor a specific directory for new files and automatically copy them to multiple destination paths. It ensures reliable file transfer and data integrity using hash verification.</p>

<h3>🔧 Key Features</h3>
<ul>
    <li><strong>Real-time File Monitoring:</strong> Uses <code>FileSystemWatcher</code> to detect new files in a specified source folder.</li>
    <li><strong>Automated File Copying:</strong> Copies detected files to predefined destination paths.</li>
    <li><strong>Retry Mechanism:</strong> Implements retry logic for file copying failures, reducing the risk of incomplete transfers.</li>
    <li><strong>File Integrity Check:</strong> Utilizes SHA-256 hashing to verify the integrity of copied files.</li>
    <li><strong>Error Logging:</strong> Logs errors and file copy statuses for debugging and monitoring.</li>
    <li><strong>Configuration via Control File:</strong> Reads destination paths from a control file (<code>startcopy.txt</code>) placed in the source directory.</li>
</ul>

<h3>📌 How It Works</h3>
<ol>
    <li>The service monitors the <code>c:\TempFiles\from</code> directory (configurable).</li>
    <li>When a file with the prefix <code>startcopy.*</code> appears, it reads <code>startcopy.txt</code> for destination paths.</li>
    <li>It attempts to copy all files from the source directory to each destination.</li>
    <li>Each copied file is verified using a hash comparison.</li>
    <li>Success and failure events are logged in <code>status.log</code> and <code>error.log</code>.</li>
</ol>

<h3>⚙️ Technologies Used</h3>
<ul>
    <li>C# (.NET Framework)</li>
    <li>Windows Service</li>
    <li>FileSystemWatcher</li>
    <li>SHA-256 Hashing</li>
</ul>

<p>🔹 This service is useful for automating file distribution across servers while ensuring data integrity.</p>
