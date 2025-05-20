#!/bin/bash
set -e

pushd dotnet
node apply-version.js
dotnet build
dotnet test FlowerBI.Engine.Tests/FlowerBI.Engine.Tests.csproj
popd

. ./publish-bootsharp.sh

pushd js
node configure-bootsharp.js
node apply-version.js
yarn
. ./fbi-release.sh
popd

pushd dotnet
# . ./push.sh
popd

pushd js
# . ./publish.sh
popd
