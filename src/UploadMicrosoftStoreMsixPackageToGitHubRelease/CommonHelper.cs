using Humanizer;

namespace UploadMicrosoftStoreMsixPackageToGitHubRelease;

public static class CommonHelper
{
    /// <summary>
    /// Pluralize the <paramref name="word"/> if <paramref name="collection"/> have more than 1 element.
    /// </summary>
    public static string PluralizeIfNeeded<T>(this string word, IReadOnlyCollection<T> collection) => collection.Count > 1 ? word.Pluralize() : word;
}
