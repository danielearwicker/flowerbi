import * as fs from "fs";
import { SemVer } from "../../SemVer.mjs";

const pattern = /\<PackageVersion\>([\d\.]+)<\/PackageVersion\>/;

const propsPath = "Directory.Build.props";

const oldConfig = fs.readFileSync(propsPath, "utf8");

const parts = oldConfig.split(pattern);

const newVersion = new SemVer(parts[1]).increasePatch();

const newConfig = [parts[0], "<PackageVersion>", newVersion, "</PackageVersion>", parts[2]].join("");

fs.writeFileSync(propsPath, newConfig);
console.log(newVersion.toString());