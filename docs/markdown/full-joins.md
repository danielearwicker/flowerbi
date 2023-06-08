# Full Joins

> This topic only applies to queries featuring multiple aggregations, and was finished in 3.10.2.

Such queries produce a separate CTE for each aggregation, because they can each have different filters. So of the availiable records, each aggregation may produce a different subset, and the subsets may overlap (or not overlap at all).

Then the CTE results are joined together by matching on all their non-aggregated columns. By default, this uses `left join`, which is unfortunate because it is not commutative, so the order of the aggregations is significant. Suppose a query selects four columns, comprising two non-aggregated and two aggregated coumns. The non-aggregated columns are categories: _Colour_ and _Flavour_. This is achieved by writing two separate queries, one for each aggregation, producing intermediate results like this:

### Left aggregation

Colour        | Flavour       | Value
--------------|---------------|------
green         | chili         | 11   
purple        | chocolate     | 42   

### Right aggregation:

Colour        | Flavour       | Value
--------------|---------------|------
purple        | chocolate     | 7   
orange        | chili         | 8

These are matched up on all the non-aggregated columns to produce:

Colour        | Flavour       | Left Value | Right Value | Combined Result
--------------|---------------|------------|-------------|----------------
green         | chili         | 11         | **_none_**  | (11, null)
purple        | chocolate     | 42         | 7           | (42, 7)
orange        | chili         | **_none_** | 8           | **_none_**

So the annoying/surprising/unsymmetrical thing is the last row, which has no combined result. The left aggregation is in control of the output: right can contribute its value to the result, but only if left provides a result for that row. Hence the last row is discarded.

So when constructing queries you would need to be careful to put the more inclusive (less filtered) aggregation first.

Ideally a `full join` should be used, which is symmetrical and the last row is no longer discarded:

Colour        | Flavour       | Left Value | Right Value | Combined Result
--------------|---------------|------------|-------------|----------------
green         | chili         | 11         | **_none_**  | (11, null)
purple        | chocolate     | 42         | 7           | (42, 7)
orange        | chili         | **_none_** | 8           | (null, 8)

This also means that the generated SQL can no longer assume that the first aggregation's columns will all be non-null. In the third row, we have to take the _Colour_ and _Flavour_ from the right aggregation, so the SQL needs to look like this (`SelectN` refers to to the Nth non-aggregated value, and `ValueM` is the Mth aggregated value):

```sql
select coalesce(a0.Select0, a1.Select0) Select0,
       coalesce(a0.Select1, a1.Select1) Select1,
       a0.Value0 Value0,
       a1.Value0 Value1
```

We can't change this now without potentially changing the results of existing queries, so there is an optional `fullJoins` flag that can be set to `true` in a query.

It is recommended that this be switched on in any query that has multiple aggregations (it has no effect on single-aggregation queries).

It is likely that in a future major version it will become the default.

