#!/bin/bash
set -e

pushd server/dotnet
node apply-version.js
dotnet build
dotnet test FlowerBI.Engine.Tests/FlowerBI.Engine.Tests.csproj
popd

pushd client
yarn

pushd packages/flowerbi
yarn prepare
popd

pushd packages/flowerbi-react
yarn prepare
popd

pushd packages/flowerbi-react-chartjs
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
