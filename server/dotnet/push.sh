export TINYBI_VERSION=`node version-up.mjs`

dotnet build

dotnet nuget push TinyBI.Engine/nupkg/TinyBI.Engine.$TINYBI_VERSION.nupkg -k $NUGET_API_KEY -s https://api.nuget.org/v3/index.json
dotnet nuget push TinyBI.Tools/nupkg/TinyBI.Tools.$TINYBI_VERSION.nupkg -k $NUGET_API_KEY -s https://api.nuget.org/v3/index.json
