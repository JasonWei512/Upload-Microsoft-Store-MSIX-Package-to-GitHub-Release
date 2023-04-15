using DebounceThrottle;
using Humanizer;
using Octokit;

namespace UploadMicrosoftStoreMsixPackageToGitHubRelease;

public class GitHubHelper
{
    private static readonly GitHubClient gitHubClient = new(new ProductHeaderValue("UploadMicrosoftStoreMsixPackageToGitHubRelease"));

    /// <summary>
    /// Set the GitHub token for authentication.
    /// </summary>
    public static void SetToken(string token)
    {
        gitHubClient.Credentials = new Credentials(token);
    }

    public static async Task<Release> GetGitHubReleaseWithVersion(string gitHubRepoOwner, string gitHubRepoName, Version msixPackageVersion)
    {
        IReleasesClient releases = gitHubClient.Repository.Release;

        try
        {
            if (await releases.GetLatest(gitHubRepoOwner, gitHubRepoName) is Release latestGitHubRelease)   // Might throw exception if not found
            {
                if (CommonHelper.MsixPackageAndGitHubReleaseVersionsAreEqual(msixPackageVersion, latestGitHubRelease.TagName))
                {
                    return latestGitHubRelease;
                }
            }
        }
        catch { }

        IReadOnlyList<Release> allGitHubReleases = await releases.GetAll(gitHubRepoOwner, gitHubRepoName);
        foreach (Release gitHubRelease in allGitHubReleases)
        {
            if (CommonHelper.MsixPackageAndGitHubReleaseVersionsAreEqual(msixPackageVersion, gitHubRelease.TagName))
            {
                return gitHubRelease;
            }
        }

        throw new Exception(@$"Cannot find GitHub release with version ""{msixPackageVersion}""");
    }

    /// <summary>
    /// Will skip the MSIX packages that have already been uploaded.
    /// </summary>
    /// <param name="dryRun">If true, do not perform the actual upload. For testing.</param>
    /// <param name="assetNamePattern">
    /// The file name pattern (without extension) of the uploaded GitHub release asset. <br/>
    /// Can contain "{version}" and "{arch}". <br/>
    /// For example, if <paramref name="assetNamePattern"/> is "App_{version}_{arch}", then this method can return "App_1.2.3.0_x64.Msix". <br/>
    /// If <paramref name="assetNamePattern"/> is null or empty, then return <paramref name="msixPackage"/>'s default file name.
    /// </param>
    public static async Task UploadMsixPackagesToGitHubRelease(Release gitHubRelease, IEnumerable<MsixPackage> msixPackages, string? assetNamePattern = null, bool dryRun = false)
    {
        List<MsixPackage> packagesToUpload = msixPackages
            .Where(
                p => !gitHubRelease.Assets.Select(a => a.Name).Contains(GetGitHubReleaseAssetName(p, assetNamePattern))
            )
            .ToList();

        Console.WriteLine($"{"file".ToQuantity(packagesToUpload.Count)} to upload.");
        Console.WriteLine();

        foreach (MsixPackage packageToUpload in packagesToUpload)
        {
            string gitHubReleaseAssetName = GetGitHubReleaseAssetName(packageToUpload, assetNamePattern);

            Console.WriteLine($"File to upload: {gitHubReleaseAssetName}");
            MsixPackageFile msixPackageFile = await StoreHelper.DownloadMsixPackageToTempDir(packageToUpload);

            using Stream fileStream = File.OpenRead(msixPackageFile.FilePath);
            long totalBytes = fileStream.Length;
            long lastTransferredBytes = 0;
            long currentTransferredBytes = -totalBytes; // The fileStream will be read twice by gitHubClient when uploading
            ThrottleDispatcher throttleDispatcher = new(1000);
            using ProgressStream.ProgressStream fileStreamWithProgress = new(fileStream, new Progress<int>(transferredChunkBytes =>
            {
                currentTransferredBytes += transferredChunkBytes;

                throttleDispatcher.Throttle(() =>
                {
                    if (currentTransferredBytes > 0)
                    {
                        string percentage = ((double)currentTransferredBytes / totalBytes * 100).ToString("00.00").PadLeft(6) + "%";
                        string totalSize = totalBytes.Bytes().Humanize();
                        string transferredSize = currentTransferredBytes.Bytes().Humanize().PadLeft(totalSize.Length);
                        string mbPerSecond = (currentTransferredBytes - lastTransferredBytes).Bytes().Humanize() + "/s";
                        lastTransferredBytes = currentTransferredBytes;

                        Console.WriteLine($"Upload Progress: {percentage}  -  {transferredSize} / {totalSize}  -  {mbPerSecond}");
                    }
                });
            }));

            ReleaseAssetUpload releaseAssetUpload = new(gitHubReleaseAssetName, "application/octet-stream", fileStreamWithProgress, null);

            if (!dryRun)
            {
                Console.WriteLine("Uploading to GitHub release ...");
                await gitHubClient.Repository.Release.UploadAsset(gitHubRelease, releaseAssetUpload);
                await Task.Delay(TimeSpan.FromMilliseconds(1500));
                Console.WriteLine($"File uploaded: {gitHubReleaseAssetName}");
            }
            else
            {
                Console.WriteLine("This is a dry run, so the file won't be uploaded.");
            }

            Console.WriteLine();
        }
    }

    /// <summary>
    /// Return the GitHub release asset file name based on <paramref name="msixPackage"/>'s version and architecture, and <paramref name="assetNamePattern"/>. <br/>
    /// <paramref name="assetNamePattern"/> can contain "{version}" and "{arch}". <br/>
    /// For example, if <paramref name="assetNamePattern"/> is "App_{version}_{arch}", then this method can return "App_1.2.3.0_x64.Msix". <br/>
    /// If <paramref name="assetNamePattern"/> is null or empty, then return <paramref name="msixPackage"/>'s default file name.
    /// </summary>
    private static string GetGitHubReleaseAssetName(MsixPackage msixPackage, string? assetNamePattern = null)
    {
        if (string.IsNullOrEmpty(assetNamePattern))
        {
            return msixPackage.FileName;
        }

        string assetName = new(assetNamePattern);
        assetName = assetName.Replace("{version}", msixPackage.Version.ToString());
        assetName = assetName.Replace("{arch}", msixPackage.Architecture);
        assetName += Path.GetExtension(msixPackage.FileName);

        return assetName;
    }
}
