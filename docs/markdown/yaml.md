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
