# TinyBI

To use this library you need to write a function that conforms to the [QueryFetch](globals.html#queryfetch) type, so it accepts a string of JSON and returns a `Promise` to a [QueryResultJson](interfaces/queryresultjson.html). The `TinyBI.Engine` library that runs inside your server/API takes care of executing the query. Your client-side function just needs to POST the JSON to your API route, along with whatever authentication details you need.

Minimal example, using normal `fetch`, and with no auth headers:

```ts
export async function localFetch(queryJson: QueryJson): Promise<QueryResultJson> {

    const response = await fetch("http://localhost:5000/query", {
        method: "POST",
        cache: "no-cache",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(queryJson),
    });

    return !response.ok ? [] : JSON.parse(await response.text(), jsonDateParser);
}
```
