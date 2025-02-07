<h2> OBS! 2 solutions - Master branch is IIS File Watcher Service
    HttpListener branch is Http listener Copy Service</h2>

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

</b>

<h2>🌐 IIS HTTP Listener File Watcher Service</h2>

<p>The <strong>IIS HTTP Listener File Watcher Service</strong> is a Windows Service that monitors a specified directory and provides a lightweight HTTP API for triggering file transfers to multiple destinations.</p>

<h3>🔧 Key Features</h3>
<ul>
    <li><strong>Embedded HTTP Server:</strong> Uses <code>HttpListener</code> to expose a REST-like interface on <code>http://localhost:5000/</code>.</li>
    <li><strong>Real-time File Monitoring:</strong> Listens for incoming POST requests to specify target destinations.</li>
    <li><strong>Automated File Copying:</strong> Transfers all files from the source directory to the specified destinations.</li>
    <li><strong>Retry Mechanism:</strong> Implements up to 3 retries for failed file transfers.</li>
    <li><strong>File Integrity Validation:</strong> Uses SHA-256 hashing to verify successful file copies.</li>
    <li><strong>Logging System:</strong> Stores success and error logs internally and serves them via GET requests.</li>
</ul>

<h3>📌 How It Works</h3>
<ol>
    <li>The service starts an <code>HttpListener</code> on <code>http://localhost:5000/</code>.</li>
    <li>A <code>GET</code> request returns the current service status and logs.</li>
    <li>A <code>POST</code> request with destination paths (semicolon-separated) triggers file copying.</li>
    <li>The service copies all files from <code>c:\TempFiles\from</code> (configurable) to the specified locations.</li>
    <li>Each copied file undergoes SHA-256 hash validation to ensure integrity.</li>
    <li>Logs are updated with success or failure details.</li>
</ol>

<h3>🛠 Example API Usage</h3>

<h4>Check Service Status</h4>
<pre><code>
GET http://localhost:5000/
</code></pre>

<h4>Trigger File Copy</h4>
<pre><code>
POST http://localhost:5000/
Content-Type: text/plain

C:\Backup1;C:\Backup2;D:\FileStorage
</code></pre>

<h3>⚙️ Technologies Used</h3>
<ul>
    <li>C# (.NET Framework)</li>
    <li>Windows Service</li>
    <li>HttpListener (Lightweight Web Server)</li>
    <li>SHA-256 Hashing</li>
    <li>Multi-threading for async operations</li>
</ul>

<h3>🚀 Use Case</h3>
<p>This service is ideal for remote-controlled file distribution via a simple HTTP interface. It is useful for automated backups, deployment pipelines, and distributed file synchronization across multiple servers.</p>
