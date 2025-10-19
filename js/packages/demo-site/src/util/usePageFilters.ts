import { useState } from "react";
import type { FilterJson } from "@flowerbi/client";
import stableStringify from "json-stable-stringify";

export interface PageFiltersState {
    readonly global: FilterJson[];
    readonly interactions: FilterJson[];
    readonly interactionKey: string;
}

export interface PageFilters extends PageFiltersState {
    setInteraction(key: string, filters: FilterJson[]): void;
    setGlobal(filters: FilterJson[]): void;
    clearInteraction(): void;
    clearGlobal(): void;
    clearAll(): void;
    getFilters(key: string): FilterJson[];
}

const clearedState: PageFiltersState = {
    global: [],
    interactions: [],
    interactionKey: "",
};

export function usePageFilters(): PageFilters {
    const [state, set] = useState<PageFiltersState>(clearedState);
    function setInteraction(
        interactionKey: string,
        interactions: FilterJson[]
    ) {
        console.log(
            interactionKey,
            state.interactionKey,
            state.interactionKey === interactionKey,
            interactions,
            state.interactions,
            stableStringify(state.interactions),
            stableStringify(interactions),
            stableStringify(state.interactions) ===
                stableStringify(interactions)
        );

        if (
            state.interactionKey === interactionKey &&
            stableStringify(state.interactions) ===
                stableStringify(interactions)
        ) {
            clearInteraction();
        } else {
            set({ ...state, interactionKey, interactions });
        }
    }
    function setGlobal(global: FilterJson[]) {
        set({ ...state, global });
    }
    function clearInteraction() {
        set({ ...state, interactionKey: "", interactions: [] });
    }
    function clearGlobal() {
        set({ ...state, global: [] });
    }
    function clearAll() {
        set(clearedState);
    }
    function getFilters(key: string) {
        const result = state.global;
        return key !== state.interactionKey
            ? result.concat(state.interactions)
            : result;
    }
    return {
        ...state,
        setInteraction,
        setGlobal,
        clearInteraction,
        clearGlobal,
        clearAll,
        getFilters,
    };
}

export interface PageFiltersProp {
    pageFilters: PageFilters;
}
