export function distinct<T>(arr: T[]) {
    const map: { [key: string]: T } = {};

    for (const item of arr) {
        map[`${item}`] = item;
    }

    return Object.values(map);
}

export function keysOf<T>(obj: T) {
    return Object.keys(obj) as (keyof T)[];
}
