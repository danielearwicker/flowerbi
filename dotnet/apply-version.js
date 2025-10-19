const fs = require("fs");

const newVersion = fs.readFileSync("../.version", "utf8").trim();

const pattern = /\<PackageVersion\>([\d\.]+)<\/PackageVersion\>/;

const propsPath = "Directory.Build.props";

const oldConfig = fs.readFileSync(propsPath, "utf8");

const parts = oldConfig.split(pattern);

const newConfig = [
    parts[0],
    "<PackageVersion>",
    newVersion,
    "</PackageVersion>",
    parts[2],
].join("");

fs.writeFileSync(propsPath, newConfig);
console.log(newVersion.toString());
