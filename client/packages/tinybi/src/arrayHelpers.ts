export function groupBy<T>(arr: T[], getKey: (i: T) => string): { [key: string]: T[] } {
    const result: { [key: string]: T[] } = {};

    for (const item of arr) {
        const key = getKey(item);
        (result[key] ?? (result[key] = [])).push(item);
    }

    return result;
}

export function distinct(arr: string[]) {
    const map: { [key: string]: boolean } = {};

    for (const item of arr) {
        map[item] = true;
    }

    return Object.keys(map);
}
