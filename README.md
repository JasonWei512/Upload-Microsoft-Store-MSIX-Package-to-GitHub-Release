# Introduction


If you publish apps to Microsoft Store and love open source, you may want to upload Microsoft Store-signed MSIX packages to GitHub release, like [Ambie](https://github.com/jenius-apps/ambie), [NanaZip](https://github.com/M2Team/NanaZip), and [Energy Star X](https://github.com/JasonWei512/EnergyStarX) do. 

Users can double click these packages to install them. No need to *install certificates to Trusted Root Certification Authorities with admin privilege* like for self-signed packages.

While the user experience is great, the developer experience is not. Every time you create a new release, you have manually to download the MSIX packages from Microsoft Store using https://store.rg-adguard.net and upload them to GitHub release.

So I created a GitHub action to automate this workflow.


# Quick Start

Add a YAML file to `.github/workflows/*.yml`.

```yaml
name: 'Upload store MSIX to release'

on: 
  schedule:
  - cron:  '0 0 */6 * * *'  # Run the action every 6 hours
  workflow_dispatch:    # Manually run the action

jobs:
  upload-store-msix-to-release:
    runs-on: ubuntu-latest

    steps:
    - uses: actions/checkout@v3

    - name: 'Upload store MSIX to release'
      uses: JasonWei512/Upload-Microsoft-Store-MSIX-Package-to-GitHub-Release@v1
      with:
        store-id: 9NF7JTB3B17P
        token: ${{ secrets.GITHUB_TOKEN }}
        asset-name-pattern: AppName_{version}_{arch}  # Optional
```


# Action Inputs

### `store-id`

The ID of the Microsoft Store app you want to upload, e.g. `9NF7JTB3B17P`.

To get the app ID:
- Open Microsoft Store and go to the app page.
- Click the share button and select "copy link".   
- A url like `https://www.microsoft.com/store/productId/9NF7JTB3B17P` will be copied to clipboard. 
- The last segment is the app ID.

### `token`

The GitHub token to use.

### `asset-name-pattern` (Optional)

The name pattern of the uploaded GitHub release asset's name without file extension. Can contain `{version}` and `{arch}`. 

For example, for pattern `AppName_{version}_{arch}`, the uploaded asset name can be `AppName_1.2.3.0_x64.Msix`.

If you don't specify an `asset-name-pattern`, the default file name from Microsoft Store will be used. For example, `14463DeveloperName.AppName_1.2.3.0_x64__23j36sa9jtp8y.msix`.