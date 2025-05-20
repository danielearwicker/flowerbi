pushd packages/@flowerbi/client
npm run fbi-release
popd

pushd packages/@flowerbi/engine
npm run fbi-release
popd

pushd packages/@flowerbi/react
npm run fbi-release
popd

pushd packages/@flowerbi/dates
npm run fbi-release
popd

pushd packages/demo-site
npm run fbi-release
popd
