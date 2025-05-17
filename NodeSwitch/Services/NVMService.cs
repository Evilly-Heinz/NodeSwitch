using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NodeSwitch.Logging;


namespace NvmManagerApp.Services
{
public static class NvmService
{
    // Static logger instance
    public static ILogger? Logger { get; set; }

    public static string NvmRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "nvm");
    public static string VersionsDir = NvmRoot;
    public static string ActiveNodePath = Path.Combine(NvmRoot, "current");

    static NvmService()
    {
        Directory.CreateDirectory(VersionsDir);
        Logger?.LogInformation("NvmService::.cctor", $"NvmService initialized. VersionsDir: {VersionsDir}");
    }
    
    public static void AddToUserPath()
    {
        Logger?.LogInformation("NvmService::AddToUserPath", "AddToUserPath called.");
        string currentDir = ActiveNodePath;

        string? existingPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);

        if (existingPath == null)
            existingPath = "";

        if (existingPath.Split(';').Any(p => p.Trim().Equals(currentDir, StringComparison.OrdinalIgnoreCase)))
        {
            Logger?.LogInformation("NvmService::AddToUserPath", "Current folder is already in the user's PATH.");
            return;
        }

        string newPath = currentDir + ";" + existingPath;

        try
        {
            Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
            Logger?.LogInformation("NvmService::AddToUserPath", $"Added {currentDir} to user's PATH.");
        }
        catch (Exception ex)
        {
            Logger?.LogError("NvmService::AddToUserPath", $"Failed to add {currentDir} to user's PATH.", ex);
        }
    }

    public static async Task InstallNodeVersionAsync(string version)
    {
        Logger?.LogInformation("NvmService::InstallNodeVersionAsync", $"InstallNodeVersionAsync called for version {version}");
        string url = $"https://nodejs.org/dist/v{version}/node-v{version}-win-x64.zip";
        string zipPath = Path.Combine(NvmRoot, $"v{version}.zip");
        string extractPath = Path.Combine(VersionsDir, $"node-v{version}-win-x64");
        string finalPath = Path.Combine(VersionsDir, 'v' + version);

        if (Directory.Exists(extractPath) || Directory.Exists(finalPath))
        {
            Logger?.LogWarning("NvmService::InstallNodeVersionAsync", $"Version {version} already exists at {extractPath} or {finalPath}");
            return;
        }

        try
        {
            using (HttpClient client = new HttpClient())
            {
                Logger?.LogInformation("NvmService::InstallNodeVersionAsync", $"Downloading Node.js {version} from {url}");
                var data = await client.GetByteArrayAsync(url);
                await File.WriteAllBytesAsync(zipPath, data);
                Logger?.LogInformation("NvmService::InstallNodeVersionAsync", $"Downloaded Node.js {version} to {zipPath}");
            }

            await ExtractToDirectoryAsync(zipPath, VersionsDir);
            Logger?.LogInformation("NvmService::InstallNodeVersionAsync", $"Extracted Node.js {version} to {VersionsDir}");

            Directory.Move(extractPath, finalPath);
            Logger?.LogInformation("NvmService::InstallNodeVersionAsync", $"Moved extracted folder from {extractPath} to {finalPath}");
        }
        catch (Exception ex)
        {
            Logger?.LogError("NvmService::InstallNodeVersionAsync", $"Failed to install Node.js version {version}", ex);
            throw;
        }
    }

    public static void UseNodeVersion(string version)
    {
        Logger?.LogInformation("NvmService::UseNodeVersion", $"UseNodeVersion called for version {version}");
        AddToUserPath();
        string versionPath = Path.Combine(VersionsDir, version);
        if (!Directory.Exists(versionPath))
        {
            Logger?.LogWarning("NvmService::UseNodeVersion", $"Version path {versionPath} does not exist.");
            return;
        }

        if (!Directory.Exists(ActiveNodePath))
            Directory.CreateDirectory(ActiveNodePath);

        string linkPath = Path.Combine(ActiveNodePath, "node.exe");
        string nodeExePath = Path.Combine(versionPath, "node.exe");
        try
        {
            if (File.Exists(linkPath))
                File.Delete(linkPath);
            File.CreateSymbolicLink(linkPath, nodeExePath);
            Logger?.LogInformation("NvmService::UseNodeVersion", $"Set active Node.js version to {version} (link: {linkPath} -> {nodeExePath})");
        }
        catch (Exception ex)
        {
            Logger?.LogError("NvmService::UseNodeVersion", $"Failed to set active Node.js version to {version}", ex);
        }
    }

    public static List<string> ListInstalledVersions()
    {
        Logger?.LogInformation("NvmService::ListInstalledVersions", "ListInstalledVersions called.");
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
        Logger?.LogInformation("NvmService::ListInstalledVersions", $"Installed versions found: {string.Join(", ", versions)}");
        return versions;
    }

    public static async Task<List<string>> ListAvailableVersionsAsync(List<string> installedVersions)
    {
        Logger?.LogInformation("NvmService::ListAvailableVersionsAsync", "ListAvailableVersionsAsync called.");
        var list = new List<string>();
        try
        {
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
            Logger?.LogInformation("NvmService::ListAvailableVersionsAsync", $"Available versions found: {string.Join(", ", list)}");
        }
        catch (Exception ex)
        {
            Logger?.LogError("NvmService::ListAvailableVersionsAsync", "Failed to list available Node.js versions.", ex);
        }
        return list;
    }

    public static void UninstallNodeVersion(string version)
    {
        Logger?.LogInformation("NvmService::UninstallNodeVersion", $"UninstallNodeVersion called for version {version}");
        string path = Path.Combine(VersionsDir, version);
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                Logger?.LogInformation("NvmService::UninstallNodeVersion", $"Uninstalled Node.js version {version} at {path}");
            }
            else
            {
                Logger?.LogWarning("NvmService::UninstallNodeVersion", $"Version {version} not found at {path}");
            }
        }
        catch (Exception ex)
        {
            Logger?.LogError("NvmService::UninstallNodeVersion", $"Failed to uninstall Node.js version {version}", ex);
        }
    }

    public static string GetActiveVersion()
    {
        Logger?.LogInformation("NvmService::GetActiveVersion", "GetActiveVersion called.");
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

            Logger?.LogInformation("NvmService::GetActiveVersion", $"Active Node.js version: {output.Trim()}");
            return output.Trim();
        }
        catch (Exception ex)
        {
            Logger?.LogError("NvmService::GetActiveVersion", "Failed to get active Node.js version.", ex);
            return "";
        }
    }

    public static async Task ExtractToDirectoryAsync(string zipPath, string extractPath)
    {
        Logger?.LogInformation("NvmService::ExtractToDirectoryAsync", $"ExtractToDirectoryAsync called. Zip: {zipPath}, Extract: {extractPath}");
        await Task.Run(() =>
        {
            try
            {
                ZipFile.ExtractToDirectory(zipPath, extractPath);
                File.Delete(zipPath);
                Logger?.LogInformation("NvmService::ExtractToDirectoryAsync", $"Extracted and deleted zip: {zipPath}");
            }
            catch (Exception ex)
            {
                Logger?.LogError("NvmService::ExtractToDirectoryAsync", $"Failed to extract zip {zipPath} to {extractPath}", ex);
                throw;
            }
        });
    }
}
}