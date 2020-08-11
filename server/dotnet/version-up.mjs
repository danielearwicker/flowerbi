import * as fs from "fs";

const newVersion = process.argv[2];
if (!newVersion) {
    console.error("Specify a new version tag", process.argv);
    process.exit(-1);
}

const pattern = /\<PackageVersion\>([\d\.]+)<\/PackageVersion\>/;

const propsPath = "Directory.Build.props";

const oldConfig = fs.readFileSync(propsPath, "utf8");

const parts = oldConfig.split(pattern);

const newConfig = [parts[0], "<PackageVersion>", newVersion, "</PackageVersion>", parts[2]].join("");

fs.writeFileSync(propsPath, newConfig);
console.log(newVersion.toString());
