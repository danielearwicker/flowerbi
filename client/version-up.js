const fs = require("fs");
const path = require("path");

const packages = fs.readdirSync("packages").filter(x => x[0] !== '.');

function getPackagePath(name) {
    return path.join("packages", name, "package.json");
}

function readPackage(name) {
    return JSON.parse(fs.readFileSync(getPackagePath(name), "utf8"));
}

function writePackage(name, json) {
    fs.writeFileSync(getPackagePath(package), JSON.stringify(json, null, 4));
}

function splitVersion(ver) {
    var ar = ver.split('.');
    if (ar.length !== 3) {
        throw new Error(`Version in bad format: ${ver}`);
    }
    return ar;
}

function compareVersions(a, b) {
    for (let n = 0; n < 3; n++) {
        const diff = a[n] - b[n];
        if (diff != 0) {
            return diff;
        }
    }
    return 0;
}

const versions = packages.map(readPackage).map(x => splitVersion(x.version));
versions.sort(compareVersions);

const newVersion = versions[versions.length - 1];
newVersion[2]++;

const newVersionStr = newVersion.join('.');

console.log(`New version is ${newVersionStr}`);

for (const package of packages) {
    
    const json = readPackage(package);
    json.version = newVersionStr;

    for (const type of ["dependencies", "devDependencies", "peerDependencies"]) {
        const deps = json[type];
        if (deps) {
            for (const other of packages) {
                if (deps[other]) {
                    deps[other] = newVersionStr;
                }
            }
        }
    }

    writePackage(package, json);
}
