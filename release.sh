pushd server/dotnet
. ./push.sh $1
popd

pushd client
. ./publish.sh $1
popd
