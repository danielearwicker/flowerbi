# YAML Schemas

You declare your FlowerBI schema by writing a simple yaml file:

```yaml
schema: TestSchema
tables:
    Vendor:
        name: Supplier # ---- DB table name can be different
        id:
            Id: [int] # ---- primary key of this table
        columns:
            VendorName: [string, SupplierName] # ---- DB column name can be different

    Department:
        id:
            Id: [int]
        columns:
            DepartmentName: [string]

    Invoice:
        id:
            Id: [int]
        columns:
            VendorId: [Vendor] # ---- foreign key, because type is another table
            DepartmentId: [Department]
            Amount: [decimal]
            Paid: [bool?] # ---- column is nullable
```

You can also optionally specify a different physical column name, by providing a second element of the array for the column, as in the `VendorName` example above, which gives `SupplierName` as the physical column name. The `name` property can also be applied to the `Schema`.

## Built-in data types

The built-in simple data types are:

-   bool
-   byte
-   short
-   int
-   long
-   float
-   double
-   decimal
-   string
-   datetime

## Foreign Keys

To define a relationship between tables we use foreign keys. In the database, this is a column of the same data type as the primary key of another table. In the yaml you just give the name of the other table as the data type of the column.

## Nullability

You can put `?` after the column's type to define it as nullable (this works for foreign keys too).

## Extending another table

If tables share a common structure you can avoid repeating yourself by using `extends`:

```yaml
Date:
    id:
        Id: [datetime]
    columns:
        CalendarYearNumber: [short]
        FirstDayOfQuarter: [datetime]
        FirstDayOfMonth: [datetime]

DateReported:
    extends: Date

DateResolved:
    extends: Date
```

In the above example, the actual DB table is called `Date`. The two `DateReported` and `DateResolved` tables are identical to `Date`, and that means they use the same physical table name `Date`. This can be very useful and is the basis of the [virtual tables](./virtual-tables.md) pattern. But if they need to refer to different physical tables that happen to share some common structure, you can add the `name` property to override the physical table name:

```yaml
DateReported:
    extends: Date
    name: DateReported
```

## Conjoint

A table can also be declared conjoint:

```yaml
InvoiceAnnotation:
    conjoint: true
    columns:
        InvoiceId: [Invoice]
        AnnotationValueId: [AnnotationValue]
```

[This is an obscure enough topic to get its own explanation](./conjoint.md).

## Meta

Tables and columns can carry an optional `meta` property: a free-form set of
name/value pairs (the value is always a string) for your own use. FlowerBI does
not interpret these values — it just carries them through so your own tooling
can read them.

```yaml
Invoice:
    meta:
        owner: finance
        pii: "false"
    id:
        Id: [int]
    columns:
        Amount:
            type: decimal
            meta:
                unit: GBP
                sensitive: "true"
```

Note that a column can only carry `meta` in its long (mapping) form — the short
`[type]` array form has nowhere to put it.

`meta` is surfaced in three places:

-   **In-memory `Schema` model** as a `Meta` property of type
    `IReadOnlyDictionary<string, string>` on every `Table` and `Column` (empty,
    never null, when none was declared).
-   **Generated TypeScript** — column meta becomes a third argument to
    `QueryColumnRuntimeType` (read it as `Invoice.Amount.type.meta`), and
    table-level meta becomes a `$meta` key on the generated table object (read it
    as `Invoice.$meta`).
-   **Generated C#** — available at runtime through the same `Schema` model, e.g.
    `BugSchema.Schema.GetTable("Invoice").Meta["owner"]`.

### Meta and `extends`

When a table `extends` another, table-level `meta` is **merged per key**: the
derived table inherits the base table's entries, and any keys it declares itself
take precedence. Columns inherited via `extends` carry their base column's `meta`
unchanged (and if the derived table redefines a column, its definition — including
its `meta` — wins entirely).

## Documentation

Tables, columns, and a top-level `topics:` section can carry free-text `doc`
and cross-reference `see` lists, surfaced in the runtime `Schema` model and in
generated TypeScript/C# as JSDoc and XML doc comments. See
[Documenting your schema](./documentation.md) for the full rules.
