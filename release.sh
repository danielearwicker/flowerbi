#!/bin/bash
set -e

pushd dotnet
node apply-version.js
dotnet build
dotnet test FlowerBI.Engine.Tests/FlowerBI.Engine.Tests.csproj
pushd FlowerBI.Bootsharp
dotnet publish -f net9.0 -c Debug
popd
popd

rm -rf js/packages/flowerbi-bootsharp
cp -R dotnet/FlowerBI.Bootsharp/bin/flowerbi-bootsharp js/packages

rm -rf js/packages/demo-site/public/_framework
cp -R dotnet/Demo/FlowerBI.WasmHost/bin/Debug/net8.0/wwwroot/_framework js/packages/demo-site/public/_framework/

pushd js
yarn
. ./fbi-release.sh
popd

pushd dotnet
. ./push.sh
popd

pushd js
. ./publish.sh
popd
