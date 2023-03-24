using Octokit;

namespace UploadMicrosoftStoreMsixPackageToGitHubRelease;

public class GitHubHelper
{
    private static readonly GitHubClient gitHubClient = new(new ProductHeaderValue("UploadMicrosoftStoreMsixPackageToGitHubRelease"));

    public static void SetToken(string token)
    {
        gitHubClient.Credentials = new Credentials(token);
    }

    public static async Task<Release> GetLatestGitHubRelease(IEnumerable<MsixPackage> msixPackages, string gitHubRepoOwner, string gitHubRepoName)
    {
        if (msixPackages.FirstOrDefault()?.Version is not string msixVersionString)
        {
            throw new Exception("Empty MSIX list");
        }

        IReleasesClient releases = gitHubClient.Repository.Release;

        try
        {
            if (await releases.GetLatest(gitHubRepoOwner, gitHubRepoName) is Release latestGitHubRelease)   // Might throw exception if not found
            {
                if (VersionsEqual(msixVersionString, latestGitHubRelease.TagName))
                {
                    return latestGitHubRelease;
                }
            }
        }
        catch { }

        IReadOnlyList<Release> allGitHubReleases = await releases.GetAll(gitHubRepoOwner, gitHubRepoName);
        foreach (Release gitHubRelease in allGitHubReleases)
        {
            if (VersionsEqual(msixVersionString, gitHubRelease.TagName))
            {
                return gitHubRelease;
            }
        }

        throw new Exception("Cannot find corresponding GitHub release");
    }

    public static async Task UploadMsixPackagesToGitHubRelease(Release gitHubRelease, IEnumerable<MsixPackage> msixPackages, string? assetNamePattern = null, bool dryRun = false)
    {
        List<MsixPackage> packagesToUpload = msixPackages
            .Where(
                p => !gitHubRelease.Assets.Select(a => a.Name).Contains(GetGitHubReleaseAssetName(p, assetNamePattern))
            )
            .ToList();

        Console.WriteLine($"{packagesToUpload.Count} {"file".PluralizeIfNeeded(packagesToUpload)} to upload.");
        Console.WriteLine();

        foreach (MsixPackage packageToUpload in packagesToUpload)
        {
            Console.WriteLine($"File to upload: {GetGitHubReleaseAssetName(packageToUpload, assetNamePattern)}");
            MsixPackageFile msixPackageFile = await StoreHelper.DownloadMsixPackageToTempDir(packageToUpload);

            using Stream fileStream = File.OpenRead(msixPackageFile.FilePath);

            ReleaseAssetUpload releaseAssetUpload = new(GetGitHubReleaseAssetName(msixPackageFile, assetNamePattern), "application/octet-stream", fileStream, null);

            if (!dryRun)
            {
                Console.WriteLine("Uploading to GitHub release ...");
                await gitHubClient.Repository.Release.UploadAsset(gitHubRelease, releaseAssetUpload);
                Console.WriteLine("File uploaded!");
            }
            else
            {
                Console.WriteLine("This is a dry run, so the file won't be uploaded.");
            }

            Console.WriteLine();
        }
    }

    /// <summary>
    /// Only compares the first 3 digits and ignores the "v" prefix in GitHub tag name.
    /// Example: "1.2.3.4" is equal to "v1.2.3".
    /// </summary>
    /// <param name="msixPackageVersion">Have 4 digits. Example: "1.2.3.4"</param>
    /// <param name="gitHubTagName">Can have a "v" prefix. Can contain 3 or 4 digits. Example: "v1.2.3"</param>
    private static bool VersionsEqual(string msixVersionString, string gitHubTagName)
    {
        Version msixVersion = new(msixVersionString);

        string gitHubVersionString = new(gitHubTagName);
        if (gitHubVersionString.StartsWith("v") || gitHubVersionString.StartsWith("V"))
        {
            gitHubVersionString = gitHubVersionString[1..];
        }
        Version gitHubVersion = new(gitHubVersionString);

        return
            msixVersion.Major == gitHubVersion.Major &&
            msixVersion.Minor == gitHubVersion.Minor &&
            msixVersion.Build == gitHubVersion.Build
            ;
    }

    private static string GetGitHubReleaseAssetName(MsixPackage msixPackage, string? assetNamePattern = null)
    {
        if (string.IsNullOrEmpty(assetNamePattern))
        {
            return msixPackage.FileName;
        }

        string assetName = new(assetNamePattern);
        assetName = assetName.Replace("{version}", msixPackage.Version);
        assetName = assetName.Replace("{arch}", msixPackage.Architecture);
        assetName += Path.GetExtension(msixPackage.FileName);

        return assetName;
    }
}
