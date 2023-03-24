using Humanizer;

namespace UploadMicrosoftStoreMsixPackageToGitHubRelease;

public static class CommonHelper
{
    public static string PluralizeIfNeeded<T>(this string word, IReadOnlyCollection<T> collection)
    {
        if (collection.Count > 1)
        {
            return word.Pluralize();
        }
        else
        {
            return word;
        }
    }
}
