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

    increasePatch() {
        return new SemVer(this.parts.slice(0, 2).concat(this.parts[2] + 1));
    }
}
