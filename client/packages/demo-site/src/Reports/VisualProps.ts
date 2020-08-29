import { QueryFetch } from "flowerbi";
import { PageFiltersProp } from "flowerbi-react";

export interface VisualProps extends PageFiltersProp {
    fetch: QueryFetch;
}
