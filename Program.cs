using System;
using System.IO;
using System.IO.Compression;
using Microsoft.Win32;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
    const string API_URL = "https://api.github.com/repos/luau-lang/luau/releases/latest";
    const string ZIP_FILE_NAME = "luau-windows.zip";

    static async Task Main(string[] args)
    {
        string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string targetFolder = Path.Combine(userFolder, ".luau");

        if (args.Length > 0 && args[0] == "--uninstall")
        {
            Uninstall(targetFolder);
            return;
        }

        string? downloadUrl = await GetLatestReleaseUrl();
        if (downloadUrl == null)
        {
            Console.WriteLine("Error: Could not fetch the latest release download URL.");
            return;
        }

        string tempZipPath = Path.Combine(Path.GetTempPath(), ZIP_FILE_NAME);

        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        DeleteAllFilesInDirectory(targetFolder);
        await DownloadFile(downloadUrl, tempZipPath);
        UnzipFile(tempZipPath, targetFolder);
        File.Delete(tempZipPath);
        AddDirectoryToPath(targetFolder);

        Console.WriteLine("Luau latest release installed successfully.");
    }

    static void Uninstall(string targetFolder)
    {
        if (Directory.Exists(targetFolder))
        {
            Directory.Delete(targetFolder, true);
            Console.WriteLine("Deleted .luau directory.");
        }

        string? currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
        if (currentPath != null && currentPath.Contains(targetFolder))
        {
            string newPath = currentPath.Replace(targetFolder + ";", "");
            Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
            Console.WriteLine("Removed .luau from PATH.");
        }

        RegistryKey? key = Registry.CurrentUser.OpenSubKey("Environment", true);
        key?.DeleteValue("PATH", false);
        key?.Close();

        Console.WriteLine("Uninstallation complete.");
    }

    static async Task<string?> GetLatestReleaseUrl()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

        try
        {
            var response = await client.GetStringAsync(API_URL);
            var releaseData = Newtonsoft.Json.Linq.JObject.Parse(response);
            foreach (var asset in releaseData["assets"]!)
            {
                if (asset["name"]?.ToString() == ZIP_FILE_NAME)
                {
                    return asset["browser_download_url"]?.ToString();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching release data: {ex.Message}");
        }
        return null;
    }

    static async Task DownloadFile(string url, string destinationPath)
    {
        var client = new HttpClient();
        using var response = await client.GetAsync(url);
        using var stream = await response.Content.ReadAsStreamAsync();
        using var file = File.OpenWrite(destinationPath);
        stream.CopyTo(file);
    }

    static void UnzipFile(string zipFilePath, string targetDirectory)
    {
        ZipFile.ExtractToDirectory(zipFilePath, targetDirectory);
    }

    static void DeleteAllFilesInDirectory(string directory)
    {
        foreach (var file in Directory.GetFiles(directory))
        {
            File.Delete(file);
        }
        foreach (var subdir in Directory.GetDirectories(directory))
        {
            Directory.Delete(subdir, true);
        }
    }

    static void AddDirectoryToPath(string directory)
    {
        string? currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User);
        if (currentPath == null || !currentPath.Contains(directory))
        {
            string newPath = (currentPath == null) ? directory : currentPath + ";" + directory;
            Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
            Console.WriteLine("Added .luau to PATH.");
        }
    }
}
