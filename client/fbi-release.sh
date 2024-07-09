node apply-version.js

pushd packages/flowerbi
npm run fbi-release
popd

pushd packages/flowerbi-react
npm run fbi-release
popd

pushd packages/flowerbi-dates
npm run fbi-release
popd

pushd packages/flowerbi-react-utils
npm run fbi-release
popd

pushd packages/demo-site
npm run fbi-release
popd
