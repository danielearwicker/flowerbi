/**
 * Returns the distinct (unique) values from an array. The comparison
 * method is very simplistic: all values are converted to strings
 * before comparison.
 * @param arr
 */
export function distinct<T>(arr: T[]) {
    const map: { [key: string]: T } = {};

    for (const item of arr) {
        map[`${item}`] = item;
    }

    return Object.values(map);
}

/**
 * Returns the names of properties (i.e. the keys) in an object, statically
 * typed so each has the string literal type of one of the properties. This
 * is not always correct, because the type will include properties inherited
 * from the prototype, where as the values returned at runtime will never
 * include inherited properties. But it's a useful approximation in situations
 * where prototype inheritance can be ignored.
 *
 * @param obj The object to obtain keys from.
 */
export function keysOf<T extends object>(obj: T) {
    return Object.keys(obj) as (keyof T)[];
}
