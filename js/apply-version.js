const fs = require("fs");
const path = require("path");

const newVersion = fs.readFileSync("../.version", "utf8").trim();

const packages = [
    "@flowerbi/bootsharp",
    "@flowerbi/client",
    "@flowerbi/dates",
    "@flowerbi/engine",
    "@flowerbi/react",
    "demo-site",
];

function getPackagePath(name) {
    return path.join("packages", name, "package.json");
}

function readPackage(name) {
    return JSON.parse(fs.readFileSync(getPackagePath(name), "utf8"));
}

function writePackage(name, json) {
    fs.writeFileSync(getPackagePath(name), JSON.stringify(json, null, 4));
}

for (const p of packages) {
    const json = readPackage(p);
    json.version = newVersion;

    for (const type of [
        "dependencies",
        "devDependencies",
        "peerDependencies",
    ]) {
        const deps = json[type];
        if (deps) {
            for (const other of packages) {
                if (deps[other]) {
                    deps[other] = newVersion;
                }
            }
        }
    }

    writePackage(p, json);
}
