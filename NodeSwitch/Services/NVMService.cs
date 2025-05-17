using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeSwitch.Services
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Net.Http;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    namespace NvmManagerApp.Services
    {
        public static class NvmService
        {
            public static string NvmRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "nvm");
            public static string VersionsDir = NvmRoot;
            public static string ActiveNodePath = Path.Combine(NvmRoot, "current");

            static NvmService()
            {
                Directory.CreateDirectory(VersionsDir);
            }

            public static void AddToUserPath()
            {
                string currentDir = ActiveNodePath;

                string? existingPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);

                if (existingPath == null)
                    existingPath = "";

                // Nếu đã tồn tại rồi thì không thêm nữa
                if (existingPath.Split(';').Any(p => p.Trim().Equals(currentDir, StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine("✅ Current folder is already in the user's PATH.");
                    return;
                }

                string newPath = currentDir + ";" + existingPath;

                try
                {
                    Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
                }
                catch
                {
                }
            }

            public static async Task InstallNodeVersionAsync(string version)
            {
                string url = $"https://nodejs.org/dist/v{version}/node-v{version}-win-x64.zip";
                string zipPath = Path.Combine(NvmRoot, $"v{version}.zip");
                string extractPath = Path.Combine(VersionsDir, $"node-v{version}-win-x64");
                string finalPath = Path.Combine(VersionsDir, 'v' + version);

                if (Directory.Exists(extractPath) || Directory.Exists(finalPath)) return;

                using (HttpClient client = new HttpClient())
                {
                    var data = await client.GetByteArrayAsync(url);
                    await File.WriteAllBytesAsync(zipPath, data);
                }


                // Replace the original line in InstallNodeVersionAsync:
                await ExtractToDirectoryAsync(zipPath, VersionsDir);
                // rename extractPath to finalPath
                Directory.Move(extractPath, finalPath);
            }

            public static void UseNodeVersion(string version)
            {
                AddToUserPath();
                string versionPath = Path.Combine(VersionsDir, version);
                if (!Directory.Exists(versionPath)) return;

                if (!Directory.Exists(ActiveNodePath))
                    Directory.CreateDirectory(ActiveNodePath);

                string linkPath = Path.Combine(ActiveNodePath, "node.exe");
                string nodeExePath = Path.Combine(versionPath, "node.exe");
                if (File.Exists(linkPath))
                    File.Delete(linkPath);
                File.CreateSymbolicLink(linkPath, nodeExePath);
            }

            public static List<string> ListInstalledVersions()
            {
                var dirs = Directory.GetDirectories(VersionsDir);
                var versions = new List<string>();
                foreach (var dir in dirs)
                {
                    var version = Path.GetFileName(dir);
                    var matched = Regex.IsMatch(version, @"v(\d+\.\d+\.\d+)");
                    if (matched)
                    {
                        versions.Add(version);
                    }
                }
                return versions;
            }

            public static async Task<List<string>> ListAvailableVersionsAsync(List<string> installedVersions)
            {
                var list = new List<string>();
                using (HttpClient client = new HttpClient())
                {
                    var html = await client.GetStringAsync("https://nodejs.org/dist/");
                    var matches = Regex.Matches(html, @"v(\d+\.\d+\.\d+)/");

                    foreach (Match match in matches)
                    {
                        var version = match.Groups[1].Value;
                        var minorVersion = int.Parse(version.Split(".")[0]);

                        if (minorVersion >= 10 && !list.Contains(version) && !installedVersions.Contains(version))
                            list.Add(version);
                    }
                }
                list.Reverse();
                return list;
            }

            public static void UninstallNodeVersion(string version)
            {
                string path = Path.Combine(VersionsDir, version);
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }

            public static string GetActiveVersion()
            {
                try
                {
                    string path = Path.Combine(ActiveNodePath, "node.exe");
                    if (!File.Exists(path))
                    {
                        path = "node";
                    }
                    var process = new System.Diagnostics.Process
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = path,
                            Arguments = "-v",
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    process.Start();
                    string output = process.StandardOutput.ReadLine() ?? "";
                    process.WaitForExit();

                    // Node.js version output is like "v18.16.0"
                    return output.Trim();
                }
                catch
                {
                    // Node is not installed or not in PATH
                    return "";
                }
            }
            // Add this async helper method to NvmService
            public static async Task ExtractToDirectoryAsync(string zipPath, string extractPath)
            {
                await Task.Run(() =>
                {
                    ZipFile.ExtractToDirectory(zipPath, extractPath);
                    File.Delete(zipPath);
                });
            }

        }
    }

}
