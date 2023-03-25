using Humanizer;

namespace UploadMicrosoftStoreMsixPackageToGitHubRelease;

public static class CommonHelper
{
    /// <summary>
    /// Pluralize the <paramref name="word"/> if <paramref name="collection"/> have more than 1 element.
    /// </summary>
    public static string PluralizeIfNeeded<T>(this string word, IReadOnlyCollection<T> collection) => collection.Count > 1 ? word.Pluralize() : word;

    /// <summary>
    /// Only compares the first 3 digits and ignores the "v" prefix in GitHub tag name. <br/>
    /// Example: <paramref name="msixPackageVersion"/> "1.2.3.4" is equal to <paramref name="gitHubReleaseTagName"/> "v1.2.3".
    /// </summary>
    /// <param name="msixPackageVersion">Have 4 digits. Example: "1.2.3.4"</param>
    /// <param name="gitHubReleaseTagName">Can have a "v" prefix. Can contain 3 or 4 digits. Example: "v1.2.3"</param>
    public static bool MsixPackageAndGitHubReleaseVersionsAreEqual(string msixPackageVersion, string gitHubReleaseTagName)
    {
        Version msixVersion;

        try
        {
            msixVersion = new Version(msixPackageVersion);
        }
        catch
        {
            // The version of the MSIX package is retrieved from Microsoft Store.
            // If it's not a valid version, something must be wrong.
            throw new Exception(@$"Invalid MSIX package version ""{msixPackageVersion}""");
        }

        string gitHubReleaseVersionString = new(gitHubReleaseTagName);
        if (gitHubReleaseVersionString.StartsWith("v") || gitHubReleaseVersionString.StartsWith("V"))
        {
            gitHubReleaseVersionString = gitHubReleaseVersionString[1..];
        }
        Version gitHubReleaseVersion;
        try
        {
            gitHubReleaseVersion = new(gitHubReleaseVersionString);
        }
        catch
        {
            return false;
        }

        return
            msixVersion.Major == gitHubReleaseVersion.Major &&
            msixVersion.Minor == gitHubReleaseVersion.Minor &&
            msixVersion.Build == gitHubReleaseVersion.Build
            ;
    }
}
