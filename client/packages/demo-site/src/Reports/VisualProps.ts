import { QueryFetch } from "tinybi";
import { PageFiltersProp } from "tinybi-react";

export interface VisualProps extends PageFiltersProp {
    fetch: QueryFetch;
}
