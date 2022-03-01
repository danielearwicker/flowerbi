#!/bin/bash
set -e

pushd server/dotnet
node apply-version.js
dotnet build
dotnet test FlowerBI.Engine.Tests/FlowerBI.Engine.Tests.csproj
popd

rm -rf client/packages/demo-site/public/_framework
cp -R server/dotnet/Demo/FlowerBI.WasmHost/bin/Debug/net6.0 client/packages/demo-site/public/_framework/

pushd client
yarn

pushd packages/flowerbi
yarn prepare
popd

pushd packages/flowerbi-react
yarn prepare
popd

pushd packages/flowerbi-dates
yarn prepare
popd

pushd packages/flowerbi-react-utils
yarn prepare
popd

pushd packages/demo-site
yarn prepare
popd

popd

pushd server/dotnet
. ./push.sh
popd

pushd client
. ./publish.sh
popd
