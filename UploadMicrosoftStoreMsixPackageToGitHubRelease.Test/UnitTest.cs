namespace UploadMicrosoftStoreMsixPackageToGitHubRelease.Test;

public class UnitTest
{
    [Theory]
    [MemberData(nameof(TestData_CommonHelper_MsixPackageAndGitHubReleaseVersionsAreEqual_True))]
    public void CommonHelper_MsixPackageAndGitHubReleaseVersionsAreEqual_True(Version msixPackageVersion, string gitHubReleaseTagName)
    {
        Assert.True(CommonHelper.MsixPackageAndGitHubReleaseVersionsAreEqual(msixPackageVersion, gitHubReleaseTagName));
    }

    public static TheoryData<Version, string> TestData_CommonHelper_MsixPackageAndGitHubReleaseVersionsAreEqual_True => new()
    {
        { new Version("1.2.3.0"), "1.2.3.0" },
        { new Version("1.2.3.0"), "1.2.3" },
        { new Version("1.2.3.1"), "1.2.3" },
        { new Version("1.2.3.0"), "v1.2.3.0" },
        { new Version("1.2.3.0"), "v1.2.3" },
        { new Version("1.2.3.1"), "v1.2.3" },
    };

    [Theory]
    [MemberData(nameof(TestData_CommonHelper_MsixPackageAndGitHubReleaseVersionsAreEqual_False))]
    public void CommonHelper_MsixPackageAndGitHubReleaseVersionsAreEqual_False(Version msixPackageVersion, string gitHubReleaseTagName)
    {
        Assert.False(CommonHelper.MsixPackageAndGitHubReleaseVersionsAreEqual(msixPackageVersion, gitHubReleaseTagName));
    }

    public static TheoryData<Version, string> TestData_CommonHelper_MsixPackageAndGitHubReleaseVersionsAreEqual_False => new()
    {
        { new Version("1.2.3.0"), "1.2.4.0" },
        { new Version("1.2.3.0"), "1.3.3" },
        { new Version("1.2.3.0"), "v2.0.0" },
        { new Version("1.2.3.0"), "invalid-tag-name" },
    };
}