import type { QueryFetch } from "@flowerbi/client";
import type { PageFiltersProp } from "../util/usePageFilters";

export interface FetchProps {
    fetch: QueryFetch;
}

export interface VisualProps extends PageFiltersProp, FetchProps {}
