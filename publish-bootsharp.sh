pushd dotnet/FlowerBI.Bootsharp
dotnet publish -f net9.0 -c Debug
popd
rm -rf js/packages/@flowerbi/bootsharp
cp -R dotnet/FlowerBI.Bootsharp/bin/@flowerbi/bootsharp js/packages/@flowerbi
