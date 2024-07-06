import { QueryFetch } from "flowerbi";
import { PageFiltersProp } from "flowerbi-react";

export interface FetchProps {
    fetch: QueryFetch;
}

export interface VisualProps extends PageFiltersProp, FetchProps {}
