using System.IO.Compression;
using Newtonsoft.Json.Linq;

class Program
{
    const string API_URL = "https://api.github.com/repos/luau-lang/luau/releases/latest";
    const string ZIP_FILE_NAME = "luau-windows.zip";
    const string INSTALLATION_FOLDER_NAME = ".luau";

    static async Task Main(string[] args)
    {
        // Stop if OS is not Windows
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("This application is designed to run only on Windows.");
            Console.ReadKey();
            return;
        }

        string userFolder = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string installationFolder = Path.Combine(userFolder, INSTALLATION_FOLDER_NAME);

        // Install if no arguments or install argument
        if (args.Length == 0 || args[0] == "--install")
        {
            await Install(installationFolder);
            Console.ReadKey();
            return;
        }

        // Update if update argument
        if (args[0] == "--update")
        {
            await Update(installationFolder);
            Console.ReadKey();
            return;
        }

        // Uninstall if uninstall argument
        if (args[0] == "--uninstall")
        {
            Uninstall(installationFolder);
            Console.ReadKey();
            return;
        }
    }

    static bool IsInstalled(string installationFolder)
    {
        return Directory.Exists(installationFolder);
    }

    static async Task Install(string installationFolder)
    {
        // Prevent if already installed
        if (IsInstalled(installationFolder))
        {
            Console.WriteLine("An existing installation was found. Aborting...");
            return;
        }

        Console.WriteLine("Beginning installation...");

        string? releaseURL = await GetReleaseURL();

        if (releaseURL == null)
        {
            Console.WriteLine("Failed to get the release URL.");
            return;
        }

        string tempZipPath = Path.Combine(Path.GetTempPath(), ZIP_FILE_NAME);

        Console.WriteLine("Downloading release...");
        await DownloadFile(releaseURL, tempZipPath);

        Console.WriteLine("Extracting files...");
        Directory.CreateDirectory(installationFolder);
        ZipFile.ExtractToDirectory(tempZipPath, installationFolder);
        File.Delete(tempZipPath);

        Console.WriteLine($"Adding {INSTALLATION_FOLDER_NAME} folder to PATH...");
        AddFolderToPath(installationFolder);

        Console.WriteLine("Installation complete.");
    }

    static void Uninstall(string installationFolder)
    {
        // Prevent if already uninstalled
        if (!IsInstalled(installationFolder))
        {
            Console.WriteLine("An installation was not found. Aborting...");
            return;
        }

        Console.WriteLine("Beginning uninstallation...");

        if (Directory.Exists(installationFolder))
        {
            Console.WriteLine($"Deleting {INSTALLATION_FOLDER_NAME} folder...");
            Directory.Delete(installationFolder, true);
        }

        Console.WriteLine($"Removing {INSTALLATION_FOLDER_NAME} folder from PATH...");
        RemoveFolderFromPath(installationFolder);

        Console.WriteLine("Uninstallation complete.");
    }

    static async Task Update(string installationFolder)
    {
        Console.WriteLine("Beginning update...");

        if (IsInstalled(installationFolder))
        {
            Uninstall(installationFolder);
        }

        await Install(installationFolder);
        Console.WriteLine("Update complete.");
    }

    static async Task<string?> GetReleaseURL()
    {
        var client = new HttpClient();

        // Needed, otherwise 403
        client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0");

        try
        {
            var response = await client.GetStringAsync(API_URL);
            var releaseData = JObject.Parse(response);

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
            Console.WriteLine(ex.Message);
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

    static void AddFolderToPath(string folder)
    {
        string currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? string.Empty;

        if (!currentPath.Contains(folder))
        {
            string newPath = string.IsNullOrEmpty(currentPath) ? folder : currentPath + ";" + folder;
            Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
        }
    }

    static void RemoveFolderFromPath(string folder)
    {
        string currentPath = Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.User) ?? string.Empty;

        if (currentPath.Contains(folder))
        {
            string newPath = currentPath.Replace(";" + folder, "").Replace(folder + ";", "").Replace(folder, "");
            Environment.SetEnvironmentVariable("PATH", newPath, EnvironmentVariableTarget.User);
        }
    }
}
