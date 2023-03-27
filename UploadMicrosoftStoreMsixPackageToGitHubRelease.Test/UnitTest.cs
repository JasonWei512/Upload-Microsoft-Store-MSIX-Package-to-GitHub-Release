namespace UploadMicrosoftStoreMsixPackageToGitHubRelease.Test;

public class UnitTest
{
    [Theory]
    [InlineData("1.2.3.0", "1.2.3.0")]
    [InlineData("1.2.3.0", "1.2.3")]
    [InlineData("1.2.3.1", "1.2.3")]
    [InlineData("1.2.3.0", "v1.2.3.0")]
    [InlineData("1.2.3.0", "v1.2.3")]
    [InlineData("1.2.3.1", "v1.2.3")]
    public void CommonHelper_MsixPackageAndGitHubReleaseVersionsAreEqual_True(string msixPackageVersion, string gitHubReleaseTagName)
    {
        Assert.True(CommonHelper.MsixPackageAndGitHubReleaseVersionsAreEqual(msixPackageVersion, gitHubReleaseTagName));
    }

    [Theory]
    [InlineData("1.2.3.0", "1.2.4.0")]
    [InlineData("1.2.3.0", "1.3.3")]
    [InlineData("1.2.3.0", "v2.0.0")]
    [InlineData("1.2.3.0", "invalid-tag-name")]
    public void CommonHelper_MsixPackageAndGitHubReleaseVersionsAreEqual_False(string msixPackageVersion, string gitHubReleaseTagName)
    {
        Assert.False(CommonHelper.MsixPackageAndGitHubReleaseVersionsAreEqual(msixPackageVersion, gitHubReleaseTagName));
    }

    [Theory]
    [InlineData("invalid-package-version", "1.2.3.0")]
    public void CommonHelper_MsixPackageAndGitHubReleaseVersionsAreEqual_Exception(string msixPackageVersion, string gitHubReleaseTagName)
    {
        Assert.Throws<Exception>(() => CommonHelper.MsixPackageAndGitHubReleaseVersionsAreEqual(msixPackageVersion, gitHubReleaseTagName));
    }
}