pushd ../dotnet/FlowerBI.Bootsharp
dotnet publish -f net9.0
popd    

rm -rf packages/flowerbi-bootsharp
cp -R ../dotnet/FlowerBI.Bootsharp/bin/flowerbi-bootsharp packages
