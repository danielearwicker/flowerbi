node version-up.mjs $1

dotnet build

dotnet nuget push TinyBI.Engine/nupkg/TinyBI.Engine.$1.nupkg -k $NUGET_API_KEY -s https://api.nuget.org/v3/index.json
dotnet nuget push TinyBI.Tools/nupkg/TinyBI.Tools.$1.nupkg -k $NUGET_API_KEY -s https://api.nuget.org/v3/index.json
