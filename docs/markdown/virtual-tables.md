Often it is useful to be able to apply extra filters to tables in a consistent way. For example, you might display several different charts implemented with different queries, but every chart should cover the same date range, or be restricted to the same department of the company.

It's simple enough to append a standard filter to every query. The problem is that very often the same dimension table can be used with multiple meanings. For example, a work item has the date it was requested and the date it was completed, so a first attempt at the [yaml schema](./yaml.md) might look like this:

```yaml
Date:
    id:
        Id: [datetime]
    columns:
        CalendarYearNumber: [short]
        FirstDayOfQuarter: [datetime]
        FirstDayOfMonth: [datetime]

WorkItem:
    id:
        Id: [int]
    columns:
        RequestedDate: [Date] # FK to Date
        CompletedDate: [Date] # Same
```

If we naively add a filter on `Date.Id`, we'll be requiring both `RequestedDate` and `CompletedDate` to match the filter, which isn't what we want. We need to decide which date our standard filter will be on. Suppose we choose `RequestedDate`; it seems we then need to set the filter on `WorkItem.RequestedDate`. But what if we have other tables that also have similar date ranges on them? We want the same common filter to be compatible with all tables that have a `RequestedDate`.

The solution is to declare virtual tables to represent the kinds of date:

```yaml
Date:
    id:
        Id: [datetime]
    columns:
        CalendarYearNumber: [short]
        FirstDayOfQuarter: [datetime]
        FirstDayOfMonth: [datetime]

DateRequested:
    extends: Date

DateCompleted:
    extends: Date

WorkItem:
    id:
        Id: [int]
    columns:
        RequestedDate: [DateRequested]
        CompletedDate: [DateCompleted]
```

The FKs now appear to reference different tables, but `DateRequested` and `DateCompleted` are not actually different physical tables in the DB. They are just logical names for use in FlowerBI queries. Now we can set a general filter on `DateRequested` and it will filter work items as expected. The difference is that when the SQL is generated from the query, there is a join from the `RequestedDate` column to the physical `Date` table that is treated as entirely separate from any such join on `CompletedDate`, so they can't get confused.

The same trick can be used for the typical many-to-many pattern using an [associative table](https://en.wikipedia.org/wiki/Associative_entity), but for more complex cases you may need [conjoint tables](./conjoint.md).
