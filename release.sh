pushd server/dotnet
. ./push.sh
popd

pushd client
. ./publish.sh
popd
