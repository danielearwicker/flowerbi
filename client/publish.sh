node apply-version.js $1

pushd packages/tinybi
npm publish
popd

pushd packages/tinybi-react
npm publish
popd

pushd packages/tinybi-react-chartjs
npm publish
popd

pushd packages/tinybi-react-utils
npm publish
popd
