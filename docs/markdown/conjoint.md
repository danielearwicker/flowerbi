# Conjoint Tables

In your [yaml schema](./yaml.md) you can optionally declare a table to be `conjoint = true`. The purpose of this feature is best explained with an example:

```yaml
InventoryItem:
    id:
        Id: [int]
    columns:
        # --- other useful columns...

AnnotationName:
    id:
        Id: [int]
    columns:
        Name: [string]

AnnotationValue:
    id:
        Id: [int]
    columns:
        AnnotationNameId: [AnnotationName]
        Value: [string]

InventoryItemAnnotation:
    columns:
        InventoryItemId: [InventoryItem]
        AnnotationValueId: [AnnotationValue]
```

Here we have a table of `InventoryItem`s and we are able to add "annotations" to them, which are simple name-value pairs. The names are expected to be heavily reused, and probably the values too. So we have a table `AnnotationName` that collects all the distinct names ever used, and also another table `AnnotationValue` that holds all the values, plus an FK to the name to which the value applies.

Finally, to link a value to an `InventoryItem`, we have `InventoryItemAnnotation` as our [associative table](https://en.wikipedia.org/wiki/Associative_entity), so that just has FKs to the item and the value.

Now suppose we want to send a query that counts how many items have a given distinct value for the annotation `shopping`. In JSON the query is simple:

```json
{
    "select": ["AnnotationValue.Value"],
    "aggregations": [
        {
            "column": "InventoryItem",
            "function": "Count"
        }
    ],
    "filters": [
        {
            "column": "AnnotationName.Name",
            "operator": "=",
            "value": "shopping"
        }
    ]
}
```

But what if we want to group by two such annotation names? We want to display a column for `math`, and then break that down further by `shopping`. There seems to be no way to write such a query. In the `select` we would be repeating `AnnotationValue.Value` twice, and then in `filters` we would be requiring `AnnotationName.Name` to have two different values.

This can be solved by using the [virtual tables](./virtual-tables.md) trick: create virtual tables for all the annotation-related tables, with name suffixes 1, 2, 3... etc. as many as we think we'll ever need. That way we can write the query as:

```json
{
    "select": ["AnnotationValue1.Value", "AnnotationValue2.Value"],
    "aggregations": [
        {
            "column": "InventoryItem",
            "function": "Count"
        }
    ],
    "filters": [
        {
            "column": "AnnotationName1.Name",
            "operator": "=",
            "value": "math"
        },
        {
            "column": "AnnotationName2.Name",
            "operator": "=",
            "value": "shopping"
        }
    ]
}
```

So this has given us a way to connect the two different filters to the two different values being selected. It works, but it's clunky. First, we can't just [extend](./yaml.md) the real tables, as two of the virtual tables need different FKs:

```yaml
AnnotationValue5:
    id:
        Id: [int]
    columns:
        AnnotationNameId: [AnnotationName5]
        Value: [string]

InventoryItemAnnotation5:
    columns:
        InventoryItemId: [InventoryItem]
        AnnotationValueId: [AnnotationValue5]
```

And second, we have to repeat this garbage N times, where N is some arbitrary limit we think will be sufficient for every query we'll need to do. Fortunately there's a better way. For all the tables that we want to virtualise, we can add the `conjoint: true` property:

```yaml
InventoryItem:
    id:
        Id: [int]
    columns:
        # ...

AnnotationName:
    conjoint: true # <--- added
    id:
        Id: [int]
    columns:
        Name: [string]

AnnotationValue:
    conjoint: true # <--- added
    id:
        Id: [int]
    columns:
        AnnotationNameId: [AnnotationName]
        Value: [string]

InventoryItemAnnotation:
    conjoint: true # <--- added
    columns:
        InventoryItemId: [InventoryItem]
        AnnotationValueId: [AnnotationValue]
```

This grants those tables auto-virtualization superpowers, allowing us to write the query as:

```json
{
    "select": ["AnnotationValue.Value@math", "AnnotationValue.Value@shopping"],
    "aggregations": [
        {
            "column": "InventoryItem.Id",
            "function": "Count"
        }
    ],
    "filters": [
        {
            "column": "AnnotationName.Name@math",
            "operator": "=",
            "value": "math"
        },
        {
            "column": "AnnotationName.Name@shopping",
            "operator": "=",
            "value": "shopping"
        }
    ]
}
```

Each place we refer to a conjoint table column, we append an identifier like `@shopping`, in the same way that we were considering adding a suffix to the table name for a virtual table. We are effectively able to conjure up unlimited virtual tables that have the correct relationships whenever we need them in a query, without needing to repetitively declare them by hand in the schema.

The suffix can be any string, with no limitations on special characters etc. because it never appears in the query, but FlowerBI behaves as if it were part of the table name. This makes automatic query generation easy, because you can incorporate values (like we've done here). Note that the `@suffix` doesn't have to match any value. We could have used `@x` and `@y`. All that matters is that the suffixes match up between the `select` and `filters` where they are meant to be referring to the same "instance" of the the conjoint tables.
