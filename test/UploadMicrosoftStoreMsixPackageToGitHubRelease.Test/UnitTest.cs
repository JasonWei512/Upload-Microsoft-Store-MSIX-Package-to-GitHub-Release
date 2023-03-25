namespace UploadMicrosoftStoreMsixPackageToGitHubRelease.Test;

public class UnitTest
{
    [Theory]
    [InlineData("1.2.3.4", "1.2.3.4")]
    [InlineData("1.2.3.4", "1.2.3")]
    [InlineData("1.2.3.4", "1.2.3.5")]
    public void CommonHelper_MsixPackageAndGitHubReleaseVersionsAreEqual_True(string msixPackageVersion, string gitHubReleaseTagName)
    {
        Assert.True(CommonHelper.MsixPackageAndGitHubReleaseVersionsAreEqual(msixPackageVersion, gitHubReleaseTagName));
    }

    [Theory]
    [InlineData("1.2.3.4", "0.1.2.3")]
    [InlineData("1.2.3.4", "invalid-tag-name")]
    public void CommonHelper_MsixPackageAndGitHubReleaseVersionsAreEqual_False(string msixPackageVersion, string gitHubReleaseTagName)
    {
        Assert.False(CommonHelper.MsixPackageAndGitHubReleaseVersionsAreEqual(msixPackageVersion, gitHubReleaseTagName));
    }

    [Theory]
    [InlineData("invalid-package-version", "1.2.3.4")]
    public void CommonHelper_MsixPackageAndGitHubReleaseVersionsAreEqual_Exception(string msixPackageVersion, string gitHubReleaseTagName)
    {
        Assert.Throws<Exception>(() => CommonHelper.MsixPackageAndGitHubReleaseVersionsAreEqual(msixPackageVersion, gitHubReleaseTagName));
    }
}