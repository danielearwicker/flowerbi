# Documenting your schema

A FlowerBI YAML schema can carry free-text documentation on tables and columns,
plus a top-level set of shared **topics** that can be referenced from anywhere.
The intended use is to give human readers (and AI agents querying the API)
context that isn't visible from column names alone — what a column means, the
units it's stored in, when not to use it, related concepts to consider.

## At a glance

```yaml
schema: TestSchema

topics:
    billing: |
        Monetary columns are in the invoice's original currency unless
        the column name ends in `USD`. Aggregating raw amounts across
        currencies is almost always a bug.
    tenancy:
        doc: Row-level security restricts visibility per user.
        see: [Customer]

tables:
    Customer:
        doc: One row per billed customer.
        see: [tenancy, Invoice]
        id:
            Id: [int]
        columns:
            CustomerName: [string]

    Invoice:
        id:
            Id: [int]
        columns:
            CustomerId: [Customer]
            Amount:
                type: decimal
                doc: Gross amount in original currency.
                see: [billing, Currency.IsoCode]
            Currency:
                type: Currency

    Currency:
        id:
            Id: [int]
        columns:
            IsoCode:
                type: string
                doc: ISO 4217 three-letter code.
```

## Adding `doc` and `see` to tables

Tables accept two optional keys alongside the existing `id`, `columns`,
`extends`, etc.:

| Key   | Type            | Meaning                                                |
| ----- | --------------- | ------------------------------------------------------ |
| `doc` | string          | Free-text explanation of the table.                    |
| `see` | list of strings | Cross-references to topics, tables, or columns.        |

Multi-line prose is best written with YAML's literal block syntax (`|`):

```yaml
Invoice:
    doc: |
        One row per finalised invoice. Pending and draft invoices live
        in the operational `InvoiceDraft` table and are not visible here.
    see: [billing, Customer]
```

## Adding `doc` and `see` to columns

A column's value can be either of two forms.

The **short form** is unchanged from earlier versions:

```yaml
Amount: [decimal, FancyAmount]
```

The **long form** is a YAML mapping, which lets you attach `doc` and `see`:

```yaml
Amount:
    type: decimal
    name: FancyAmount        # optional, same role as the second element of the short form
    doc: Gross amount in original currency.
    see: [billing, Currency.IsoCode]
```

Only `type` is required. You can mix short and long forms freely within the
same `columns:` block.

## Shared topics

The top-level `topics:` section defines named snippets of prose that can be
referenced from anywhere via a `see:` list. Each entry can be either a plain
string (just the prose) or a mapping with `doc` and an optional `see` of its
own — topics can themselves cross-reference other topics, tables, or columns.

```yaml
topics:
    # Short form: just prose
    tenancy: |
        Row-level security restricts visibility per user.

    # Long form: prose plus its own cross-references
    billing:
        doc: |
            Monetary columns are in the invoice's original currency unless
            the column name ends in `USD`.
        see: [Currency.IsoCode]
```

Topic names share a namespace with table names — **a topic must not have the
same name as a table**. The schema parser raises an error if they collide.
This rule is what lets a bare name in a `see:` list resolve unambiguously.

## How `see` references resolve

Each entry in a `see:` list is interpreted as follows:

| Form          | Meaning                                                            |
| ------------- | ------------------------------------------------------------------ |
| `Customer`    | A topic if a topic of that name exists, otherwise a table.         |
| `Customer.Id` | The column `Id` on table `Customer`.                               |

References are resolved at schema load time. An unknown name is an error.

## Inheritance via `extends`

When a table uses `extends`, it inherits the parent's `doc` and `see` (and per
column, the inherited columns inherit the parent column's `doc` and `see`)
unless the child supplies its own. Setting `see: []` explicitly clears the
inherited list.

```yaml
DateReported:
    extends: Date
    doc: The date a bug was first reported.   # overrides Date's doc
    # see: is inherited from Date
```

## What the runtime model exposes

`Schema.FromYaml(...)` builds a runtime model in which:

-   `Table` and every `IColumn` implement `IDocumented`, exposing `Doc` and
    `See` (a list of `IDocumented`).
-   `Topic` is a separate type, also `IDocumented`. Its `Doc` is the topic's
    prose and its `See` is its own cross-references.
-   `Schema.Topics` is an `IReadOnlyDictionary<string, Topic>`.
-   Every `See` list is fully resolved — entries are the actual `Table`,
    `IColumn`, or `Topic` objects, not strings — so consumers (e.g. an
    AI-facing introspection endpoint) can walk the graph without further
    lookups.

```csharp
var schema = Schema.FromYaml(yaml);
var amount = schema.GetTable("Invoice").GetColumn("Amount");

Console.WriteLine(amount.Doc);
foreach (var related in amount.See)
{
    Console.WriteLine($"  see also {related.RefName} ({related.GetType().Name})");
}

foreach (var topic in schema.Topics.Values)
{
    Console.WriteLine($"{topic.RefName}: {topic.Doc}");
}
```

## Generated TypeScript and C#

`FlowerBI.Tools` emits `doc` and `see` annotations into both generated outputs.

### TypeScript (JSDoc)

```ts
/**
 * One row per billed customer.
 * @see {@link Topics.tenancy}
 * @see {@link Invoice}
 */
export const Customer = {
    /**
     * Display name.
     */
    CustomerName: new StringQueryColumn<string>("Customer.CustomerName", ...),
};

export const Topics = {
    /**
     * Monetary columns are in the invoice's original currency...
     */
    billing: "billing",
    ...
};
```

Topic references are rewritten to `Topics.<name>` (or `Topics["..."]` for
non-identifier names) so that IDEs can resolve the link.

### C# (XML doc)

```csharp
/// <summary>One row per billed customer.</summary>
/// <seealso cref="Topics.tenancy"/>
/// <seealso cref="Invoice"/>
public static class Customer
{
    /// <summary>Display name.</summary>
    public const string CustomerName = "Customer.CustomerName";
}

public static class Topics
{
    /// <summary>Monetary columns are in the invoice's original currency...</summary>
    public const string billing = "billing";
}
```

Topic identifiers are sanitised for C# (e.g. `currency-codes` becomes
`currency_codes`) but the constant value is the original name, so it can be
used as a key into `Schema.Topics` at runtime.
