# TinyBI

At its core, TinyBI is a pattern for supporting querying of star-schema through a single POST route, so that clients can enough power to do queries that involve simple aggregation and joins, but the server/API can can carefully limit what clients are able to do.

It focuses on supporting very succinct query definitions at the client, and strong-typing via TypeScript inference (helped by little bit of metaprogramming).

## Use Case

Our users' data is:

- in SQL Server databases,
- arranged in a [star schema](https://en.wikipedia.org/wiki/Star_schema),
- served up by our own API (dotnet core/C#),
- subject to fine-grained row-level authorisation (that is, only some users are allowed to see some rows).

Our UI is:

- written in TypeScript/React,
- required to show aggregated statistics in nice-looking charts,
- driven by rapid evolution and enhancement from user feedback.

We've tried using a paid no-code BI product, and while it had some severe drawbacks, we liked the way it worked with data:

- In one central place, you describe the schema: a _fact_ table that has several foreign keys to _dimension_ tables, all many-to-one relationships.
- For each chart you want to render, you just specify columns to group by (from any table) and columns to aggregate over (count, sum, etc.)
- Something builds the SQL query for you - this part isn't really that hard, but it's very productive to have it automated.

There are a lot of free libraries for drawing charts: [chart.js](https://www.chartjs.org/) is really easy to use, and it has [a good React wrapper](https://github.com/jerairrest/react-chartjs-2).

But it would be a drag to have to write a separate API route that runs a different specific SQL query to get the data for each chart. TinyBI takes away that problem, and there's honestly not a lot to it.

## Flexible and creative querying at the client

Define the shape of the data you need in your client code:

```ts
const { records } = useQuery(fetch, {
    select: {
        customer: Customer.CustomerName,
        bugCount: Bug.Id.count()
    },
    filters: [ Workflow.Resolved.equalTo(true) ]
});
```

The `useQuery` function is a handy React hook, defined in the `tinybi-react` npm package, but the core client code in the `tiny-bi` package has no dependency on React, so it's not a prerequisite.

You supply the `fetch` function to call your API, with your choice of authentication. Inside your API you pass a chunk of JSON to `TinyBI.Engine` and it performs the SQL query.

## Easy mapping to widely-used visualisation libraries

Render the returned records easily in React, maybe using [chart.js](https://github.com/jerairrest/react-chartjs-2):

```tsx
<Pie data={{
    labels: records.map(x => x.customer),
    datasets: [{
        label: "Bugs",
        data: result.records.map(x => x.bugCount)
    }]
}} />
```

The record fields `customer` and `bugCount` are strongly typed, inferred from the `select` in the query.

## Lock down the schema

Obviously it's not safe to allow clients to send raw SQL to an API and get it executed, so that's not happening here. The query refers to tables/columns (such as `Customer.CustomerName`) that are defined inside the API, declared in C# like this:

```cs
[DbTable("Customer")]
public static class Customer
{
    public static readonly PrimaryKey<int> Id = new PrimaryKey<int>("Id");
    public static readonly Column<string> CustomerName = new Column<string>("CustomerName");
}

[DbTable("Bug")]
public static class Bug
{
    public static readonly PrimaryKey<int> Id = new PrimaryKey<int>("Id");
    public static readonly ForeignKey<int> CustomerId = new ForeignKey<int>("CustomerId", Customer.Id);
    // other columns
}
```

`TinyBI.Tools` automatically reflects over this structure and generates a TypeScript file that the client can use to get auto-completion and type inference in its queries. So the client code can query the data in a creative and flexible way, but only within the boundaries set by your API's schema definition. Your API can also easily add extra filters to the query, to impose "row-level security" on a per-user basis.

## Automatic joins, grouping and aggregation

Our example query casually takes from two different tables:

```ts
select: {
    customer: Customer.CustomerName,
    bugCount: Bug.Id.count()
}
```

The schema tells us that `Bug` has a foreign key `CustomerId` to the `Customer` table, so we're going to need a `join`. Also the `bugCount` column uses an aggregation function, `count`, which means that we're going to `group by` the customer name to get the number of bugs reported per customer.

You can specify any number of plain columns (strings, numbers, dates, booleans) to group by, and any number of numeric columns with an aggregation function (`count`, `sum`, `average`).

You can also optionally supply different filters to each aggregation, so e.g. get the count of all bugs, and the count of resolved bugs, per customer.

```ts
select: {
    customer: Customer.CustomerName,
    countAllBugs: Bug.Id.count(),
    countResolvedBugs: Bug.Id.count([Workflow.Resolved.equalTo(true)])
}
```

The result has records like this:

```ts
{
    customer: string,
    countAllBugs: number,
    countResolvedBugs: number
}
```

Perfect for mapping to a multi-bar chart.

## Live Demo

https://earwicker.com/tinybi/demo/

This runs the whole stack in-browser, using some WASM-based components. This is not part of the real solution; no WASM is needed. It's just a way to run a live demo without having to pay to run real boxes!

- [sql.js](https://github.com/sql-js/sql.js) representing the RDBMS
- The dotnet core engine built in [Blazor](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor), representing an application server
- The UI "fetches" data from the Blazor app, which uses `TinyBI.Engine` to generate SQL queries and runs them against sql.js. It does some ugly hackery to make the queries compatible, as they are currently generated to target Microsoft SQL Server which has a different syntax for many basic things.

## Reference Documentation

Gradually appearing:

- [tinybi](https://earwicker.com/tinybi/typedoc/tinybi)
- [tinybi-react](https://earwicker.com/tinybi/typedoc/tinybi-react)
- [tinybi-react-chartjs](https://earwicker.com/tinybi/typedoc/tinybi-react-chartjs)
- [tinybi-react-utils](https://earwicker.com/tinybi/typedoc/tinybi-react-utils)
