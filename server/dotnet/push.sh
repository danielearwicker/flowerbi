export VER=`cat ../../.version`

dotnet nuget push TinyBI.Engine/nupkg/TinyBI.Engine.$VER.nupkg -k $NUGET_API_KEY -s https://api.nuget.org/v3/index.json
dotnet nuget push TinyBI.Tools/nupkg/TinyBI.Tools.$VER.nupkg -k $NUGET_API_KEY -s https://api.nuget.org/v3/index.json
