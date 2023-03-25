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

    public static async Task<Release> GetGitHubReleaseWithVersion(string gitHubRepoOwner, string gitHubRepoName, string msixPackageVersion)
    {
        IReleasesClient releases = gitHubClient.Repository.Release;

        try
        {
            if (await releases.GetLatest(gitHubRepoOwner, gitHubRepoName) is Release latestGitHubRelease)   // Might throw exception if not found
            {
                if (VersionsEqual(msixPackageVersion, latestGitHubRelease.TagName))
                {
                    return latestGitHubRelease;
                }
            }
        }
        catch { }

        IReadOnlyList<Release> allGitHubReleases = await releases.GetAll(gitHubRepoOwner, gitHubRepoName);
        foreach (Release gitHubRelease in allGitHubReleases)
        {
            if (VersionsEqual(msixPackageVersion, gitHubRelease.TagName))
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
    /// Only compares the first 3 digits and ignores the "v" prefix in GitHub tag name. <br/>
    /// Example: <paramref name="msixPackageVersion"/> "1.2.3.4" is equal to <paramref name="gitHubReleaseTagName"/> "v1.2.3".
    /// </summary>
    /// <param name="msixPackageVersion">Have 4 digits. Example: "1.2.3.4"</param>
    /// <param name="gitHubReleaseTagName">Can have a "v" prefix. Can contain 3 or 4 digits. Example: "v1.2.3"</param>
    private static bool VersionsEqual(string msixPackageVersion, string gitHubReleaseTagName)
    {
        Version msixVersion = new(msixPackageVersion);

        string gitHubReleaseVersionString = new(gitHubReleaseTagName);
        if (gitHubReleaseVersionString.StartsWith("v") || gitHubReleaseVersionString.StartsWith("V"))
        {
            gitHubReleaseVersionString = gitHubReleaseVersionString[1..];
        }
        Version gitHubReleaseVersion = new(gitHubReleaseVersionString);

        return
            msixVersion.Major == gitHubReleaseVersion.Major &&
            msixVersion.Minor == gitHubReleaseVersion.Minor &&
            msixVersion.Build == gitHubReleaseVersion.Build
            ;
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
        assetName = assetName.Replace("{version}", msixPackage.Version);
        assetName = assetName.Replace("{arch}", msixPackage.Architecture);
        assetName += Path.GetExtension(msixPackage.FileName);

        return assetName;
    }
}
