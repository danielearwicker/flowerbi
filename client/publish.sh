node apply-version.js

pushd packages/flowerbi
npm publish
popd

pushd packages/flowerbi-react
npm publish
popd

pushd packages/flowerbi-dates
npm publish
popd

pushd packages/flowerbi-react-utils
npm publish
popd
