export class SemVer {
    constructor(strOrArr) {        
        this.parts = Array.isArray(strOrArr) ? strOrArr 
            : strOrArr.split('.').map(x => parseInt(x, 10));

        if (this.parts.length !== 3) {
            throw new Error(`Version in bad format: ${strOrArr}`);
        }
    }

    toString() {
        return this.parts.join(".");
    }

    compare(other) {
        for (let n = 0; n < 3; n++) {
            const diff = this.parts[n] - other.parts[n];
            if (diff != 0) {
                return diff;
            }
        }
        return 0;
    }

    increase(part) {
        if (typeof part === "string") {
            part = part === "major" ? 0 :
                   part === "minor" ? 1 :
                   part === "patch" ? 2 : part;
        }

        if (typeof part !== "number" || part > 2 || part < 0) {
            throw new Error("SemVer.patch accepts: major | minor | patch");
        }

        return new SemVer(this.parts.map((v, p) =>
            p < part ? v :
            p > part ? 0 :
            v + 1));            
    }
}
