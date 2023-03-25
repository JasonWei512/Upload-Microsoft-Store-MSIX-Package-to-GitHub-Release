using DebounceThrottle;
using Downloader;
using Humanizer;
using StoreLib.Models;
using StoreLib.Services;

namespace UploadMicrosoftStoreMsixPackageToGitHubRelease;

/// <summary>
/// An MSIX package on Microsoft Store, which hasn't been downloaded to disk.
/// </summary>
public record MsixPackage(string Moniker, string Version, string Architecture, string DownloadUrl, string FileName);

/// <summary>
/// A <see cref="MsixPackage"/> which is already downloaded to <paramref name="FilePath"/>.
/// </summary>
public record MsixPackageFile(string Moniker, string Version, string Architecture, string DownloadUrl, string FileName, string FilePath)
    : MsixPackage(Moniker, Version, Architecture, DownloadUrl, FileName);

public static class StoreHelper
{
    /// <summary>
    /// Get the list of the latest MSIX packages for different architectures (x64, arm64, etc.) from Microsoft Store without downloading them.
    /// </summary>
    /// <param name="storeID">The ID of the Microsoft Store app you want to upload, e.g. "9NF7JTB3B17P".</param>
    public static async Task<IReadOnlyList<MsixPackage>> GetLatestMsixPacakgeList(string storeID)
    {
        DisplayCatalogHandler dcathandler = new(DCatEndpoint.Production, new Locale(Market.US, Lang.en, false));

        Console.WriteLine($@"Getting info of app with ID ""{storeID}"" from Microsoft Store ...");

        try
        {
            await dcathandler.QueryDCATAsync(storeID);
        }
        catch (Exception e)
        {
            throw new Exception($@"App with ID ""{storeID}"" not found.", e);
        }

        if (!dcathandler.IsFound)
        {
            return new List<MsixPackage>();
        }

        if (dcathandler.ProductListing.Product.DisplaySkuAvailabilities.FirstOrDefault()?.Sku.LocalizedProperties.FirstOrDefault()?.SkuTitle is string appName)
        {
            Console.WriteLine($"Found app: {appName}");
        }

        Console.WriteLine();

        List<Package> allPackages = dcathandler.ProductListing.Product.DisplaySkuAvailabilities
            .SelectMany(x => x.Sku.Properties.Packages)
            .ToList();

        if (allPackages.FirstOrDefault()?.PackageFamilyName is not string pacakgeFamilyName)
        {
            return new List<MsixPackage>();
        }

        if (allPackages.Max(x => x.Version) is not string latestVersion)
        {
            return new List<MsixPackage>();
        }

        List<string> latestPackageFullNames = allPackages
            .Where(x => x.Version == latestVersion)
            .Select(x => x.PackageFullName)
            .Distinct()
            .ToList();

        Console.WriteLine("Getting app package list ...");

        IEnumerable<PackageInstance> packageInstances = (await dcathandler.GetPackagesForProductAsync())
            .Where(x => latestPackageFullNames.Contains(x.PackageMoniker));

        List<MsixPackage> msixPackages = new();

        foreach (PackageInstance packageInstance in packageInstances)
        {
            string downloadUrl = packageInstance.PackageUri.ToString();

            string fileName = await GetFileNameFromDownloadUrl(downloadUrl);

            string[] monikerSplits = packageInstance.PackageMoniker.Split("_");

            MsixPackage msixPackage = new(
                packageInstance.PackageMoniker,
                monikerSplits[1],
                monikerSplits[2],
                downloadUrl,
                fileName
            );

            msixPackages.Add(msixPackage);
        }

        Console.WriteLine($"The latest app version is: {msixPackages.FirstOrDefault()?.Version}");
        Console.WriteLine($"Found {msixPackages.Count} {"package".PluralizeIfNeeded(msixPackages)}: {string.Join(", ", msixPackages.Select(x => x.Architecture))}");
        Console.WriteLine();

        return msixPackages;
    }

    /// <summary>
    /// Returns an <see cref="MsixPackageFile"/> representing the downloaded MSIX package file.
    /// </summary>
    public static async Task<MsixPackageFile> DownloadMsixPackageToTempDir(MsixPackage msixPackage)
    {
        Console.WriteLine($"Downloading {msixPackage.Architecture} package from Microsoft Store ...");

        string filePath = await DownloadFileToTempDir(msixPackage.DownloadUrl);

        MsixPackageFile msixPackageFile = new(
            msixPackage.Moniker,
            msixPackage.Version,
            msixPackage.Architecture,
            msixPackage.DownloadUrl,
            msixPackage.FileName,
            filePath
        );

        await Task.Delay(TimeSpan.FromMilliseconds(1500));
        Console.WriteLine("File downloaded to: " + filePath);

        return msixPackageFile;
    }

    /// <summary>
    /// Get file name from <paramref name="url"/> without downloading it. 
    /// </summary>
    private static Task<string> GetFileNameFromDownloadUrl(string url)
    {
        DownloadService downloader = new();

        TaskCompletionSource<string> taskCompletionSource = new();

        downloader.DownloadStarted += (s, e) =>
        {
            string fileName = Path.GetFileName(e.FileName);
            downloader.CancelTaskAsync().ContinueWith(t => downloader.Dispose());
            taskCompletionSource.SetResult(fileName);
        };

        downloader.DownloadFileTaskAsync(url, new DirectoryInfo(Path.GetTempPath()));

        return taskCompletionSource.Task;
    }

    /// <summary>
    /// Returns the path of the downloaded file.
    /// </summary>
    private static Task<string> DownloadFileToTempDir(string url)
    {
        string fileName = string.Empty;

        DownloadService downloader = new();

        downloader.DownloadStarted += (s, e) =>
        {
            fileName = e.FileName;
        };

        ThrottleDispatcher throttleDispatcher = new(1000);

        downloader.DownloadProgressChanged += (s, e) =>
        {
            throttleDispatcher.Throttle(() =>
            {
                Console.WriteLine(PrettifyDownloadProgress(e));
            });
        };

        TaskCompletionSource<string> taskCompletionSource = new();

        downloader.DownloadFileCompleted += (s, e) =>
        {
            taskCompletionSource.SetResult(fileName);
            downloader.Dispose();
        };

        downloader.DownloadFileTaskAsync(url, new DirectoryInfo(Path.GetTempPath()));

        return taskCompletionSource.Task;
    }

    private static string PrettifyDownloadProgress(DownloadProgressChangedEventArgs progress)
    {
        string percentage = progress.ProgressPercentage.ToString("00.00").PadLeft(6) + "%";
        string totalSize = progress.TotalBytesToReceive.Bytes().Humanize();
        string receivedSize = progress.ReceivedBytesSize.Bytes().Humanize().PadLeft(totalSize.Length);
        string mbPerSecond = progress.BytesPerSecondSpeed.Bytes().Humanize() + "/s";

        return $"Progress: {percentage}  -  {receivedSize} / {totalSize}  -  {mbPerSecond}";
    }
}
