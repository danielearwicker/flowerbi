const fs = require("fs");

const fileName = "packages/@flowerbi/bootsharp/package.json";

fs.writeFileSync(
    fileName,
    JSON.stringify(
        {
            ...JSON.parse(fs.readFileSync(fileName, "utf8")),
            author: "Daniel Earwicker <dan@earwicker.com>",
            license: "MIT",
            description:
                "FlowerBI: ultra-minimal BI analytics query and UI tools",
        },
        null,
        4
    )
);
