export VER=`cat ../.version`

dotnet nuget push FlowerBI.Engine/nupkg/FlowerBI.Engine.$VER.nupkg -k $NUGET_API_KEY -s https://api.nuget.org/v3/index.json
dotnet nuget push FlowerBI.Tools/nupkg/FlowerBI.Tools.$VER.nupkg -k $NUGET_API_KEY -s https://api.nuget.org/v3/index.json
