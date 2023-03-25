# Set the base image as the .NET 7.0 SDK (this includes the runtime)
FROM mcr.microsoft.com/dotnet/sdk:7.0 as build-env

# Copy everything and publish the release (publish implicitly restores and builds)
WORKDIR /build
COPY . .
WORKDIR /build/UploadMicrosoftStoreMsixPackageToGitHubRelease
RUN dotnet publish -c Release -o /app --no-self-contained

# Label the container
LABEL maintainer="Jason Wei <JasonWei512@Outlook.com>"
LABEL repository="https://github.com/JasonWei512/Upload-Microsoft-Store-MSIX-Package-to-GitHub-Release"
LABEL homepage="https://github.com/JasonWei512/Upload-Microsoft-Store-MSIX-Package-to-GitHub-Release"

# Label as GitHub action
LABEL com.github.actions.name="Upload Microsoft Store MSIX Package to GitHub Release"
# Limit to 160 characters
LABEL com.github.actions.description="Download MSIX package from Microsoft Store and upload to GitHub release."
# See branding:
# https://docs.github.com/actions/creating-actions/metadata-syntax-for-github-actions#branding
LABEL com.github.actions.icon="upload"
LABEL com.github.actions.color="blue"

# Relayer the .NET SDK, anew with the build output
FROM mcr.microsoft.com/dotnet/runtime:7.0
COPY --from=build-env /app /app
ENTRYPOINT [ "dotnet", "/app/UploadMicrosoftStoreMsixPackageToGitHubRelease.dll" ]