# Dependencies

- [StoreDev/StoreLib](https://github.com/StoreDev/StoreLib) ([3f2a98f](https://github.com/StoreDev/StoreLib/commit/3f2a98ffede0bf3f78321c194e884fd0aaf14c29)) for getting MSIX package download links of Microsoft Store apps

## Why not nuget / git submodule?

StoreLib's [nuget package](https://www.nuget.org/packages/StoreLib) is pretty outdated. 

It was last updated in 2020, while the GitHub repo was last updated in 2022. 

The nuget pakcage lacks some features like getting the download links of arm64 MSIX packages, which the GitHub repo has.

See: https://github.com/StoreDev/StoreLib/issues/30

And git submodule doesn't work well with "Docker" type GitHub actions. See:

https://github.com/orgs/community/discussions/50895