import * as fs from "fs";
import * as path from "path";

const newVersion = process.argv[2];
if (!newVersion) {
    console.error("Specify a new version tag", process.argv);
    process.exit(-1);
}

const packages = fs.readdirSync("packages").filter(x => x[0] !== '.');

function getPackagePath(name) {
    return path.join("packages", name, "package.json");
}

function readPackage(name) {
    return JSON.parse(fs.readFileSync(getPackagePath(name), "utf8"));
}

function writePackage(name, json) {
    fs.writeFileSync(getPackagePath(name), JSON.stringify(json, null, 4));
}

console.log(`New version is ${newVersion}`);

for (const p of packages) {
    
    const json = readPackage(p);
    json.version = newVersion;

    for (const type of ["dependencies", "devDependencies", "peerDependencies"]) {
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
