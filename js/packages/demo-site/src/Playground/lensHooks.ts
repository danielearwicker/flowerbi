import { useCallback } from "react";

export type UpdaterFunc<T> = T | ((previous: T) => T);
export type SetterFunc<T> = (updater: UpdaterFunc<T>) => void;

export function getUpdatedValue<T>(current: T, updaterFunc: UpdaterFunc<T>) {
    if (typeof updaterFunc === "function") {
        const u = updaterFunc as unknown as (previous: T) => T;
        return u(current);
    }
    return updaterFunc;
}

export function useLensOnArray<I>(current: I[], setter: SetterFunc<I[]>, index: number, create: () => I, deletion: (from: I, to: I) => boolean) {
    if (index >= current.length) {
        throw new Error("useLensOnArray: index is greater than array length");
    }

    const itemSetter = useCallback(
        (itemUpdater: UpdaterFunc<I>) => {
            setter((previousList: I[]) => {
                const previousItem = previousList[index] ?? create();
                const updatedItem = getUpdatedValue(previousItem, itemUpdater);
                const updatedList = previousList.slice();

                if (deletion(previousItem, updatedItem)) {
                    updatedList.splice(index, 1);
                } else {
                    updatedList[index] = updatedItem;
                }

                return updatedList;
            });
        },
        [setter, create, deletion, index]
    );

    return [current[index] ?? create(), itemSetter] as const;
}

export function useLensOnObject<O, P extends keyof O>(current: O, setter: SetterFunc<O>, key: P) {
    const propertySetter = useCallback(
        (propertyUpdater: UpdaterFunc<O[P]>) => {
            setter((previousObject: O) => {
                return {
                    ...previousObject,
                    [key]: getUpdatedValue(previousObject[key], propertyUpdater),
                };
            });
        },
        [setter, key]
    );
    return [current[key], propertySetter] as const;
}
