#!/bin/bash
set -e

pushd server/dotnet
node apply-version.js
dotnet build
dotnet test FlowerBI.Engine.Tests/FlowerBI.Engine.Tests.csproj
popd

rm -rf client/packages/demo-site/public/_framework
cp -R server/dotnet/Demo/FlowerBI.WasmHost/bin/Debug/net8.0/wwwroot/_framework client/packages/demo-site/public/_framework/

pushd client
yarn
. ./fbi-release.sh
popd

pushd server/dotnet
. ./push.sh
popd

pushd client
. ./publish.sh
popd
