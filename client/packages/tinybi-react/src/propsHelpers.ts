export type IdOrTitle = {
    id?: string; 
    title?: string;
}

export function getIdAndTitle(props: IdOrTitle) {
    const id = props.id ?? props.title?.replace(/\s+/g, "");
    const title = props.title ?? "";

    if (!id || !title) {
        throw new Error("Need to specify at least one of id and title");
    }

    return { id: id, title: title };
}

export type RequireIdOrTitle<T> =
    (T & { id: string; title?: string; }) |
    (T & { id?: string; title: string; });
