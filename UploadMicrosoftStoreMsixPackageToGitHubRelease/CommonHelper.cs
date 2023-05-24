namespace UploadMicrosoftStoreMsixPackageToGitHubRelease;

public static class CommonHelper
{
    /// <summary>
    /// Only compares the first 3 digits and ignores the "v" prefix in GitHub tag name. <br/>
    /// Example: <paramref name="msixPackageVersion"/> "1.2.3.4" is equal to <paramref name="gitHubReleaseTagName"/> "v1.2.3".
    /// </summary>
    /// <param name="msixPackageVersion">Have 4 digits. Example: "1.2.3.4"</param>
    /// <param name="gitHubReleaseTagName">Can have a "v" prefix. Can contain 3 or 4 digits. Example: "v1.2.3"</param>
    public static bool MsixPackageAndGitHubReleaseVersionsAreEqual(Version msixPackageVersion, string gitHubReleaseTagName)
    {
        string gitHubReleaseVersionString = new(gitHubReleaseTagName);
        if (gitHubReleaseVersionString.StartsWith("v") || gitHubReleaseVersionString.StartsWith("V"))
        {
            gitHubReleaseVersionString = gitHubReleaseVersionString[1..];
        }
        Version gitHubReleaseVersion;
        try
        {
            gitHubReleaseVersion = new Version(gitHubReleaseVersionString);
        }
        catch
        {
            return false;
        }

        return
            msixPackageVersion.Major == gitHubReleaseVersion.Major &&
            msixPackageVersion.Minor == gitHubReleaseVersion.Minor &&
            msixPackageVersion.Build == gitHubReleaseVersion.Build
            ;
    }
}