import { useState } from "react";
import { FilterJson } from "tinybi";

export interface PageFiltersState {
    readonly filters: FilterJson[];
    readonly chartKey: string;
}

export interface PageFilters extends PageFiltersState {
    set(newState: PageFiltersState): void;
    clear(): void;
}

const clearedState: PageFiltersState = {
    filters: [],
    chartKey: "",
};

export function usePageFilters(): PageFilters {
    const [state, set] = useState<PageFiltersState>(clearedState);
    function clear() {
        set(clearedState);
    }
    return { ...state, set, clear };
}

export interface PageFiltersProp {
    pageFilters: PageFilters;
}
