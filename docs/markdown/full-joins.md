# Full Joins

> This topic only applies to queries featuring multiple aggregations, and was finished in 3.10.2.

Such queries produce a separate CTE for each aggregation, because they can each have different filters. So of the availiable records, each aggregation may produce a different subset, and the subsets may overlap (or not overlap at all).

Then the CTE results are joined together by matching on all their non-aggregated columns. By default, this uses `left join`, which is unfortunate because it is not commutative, so the order of the aggregations is significant:

Left          | Right         | Combined Result
--------------|---------------|----------------
2             | 3             | (2, 3)
8             | _missing_     | (8, null)
_missing_     | 5             | **_missing_**

The left aggregation is in control of the output: right can contribute its value to the result, but only if left provides a result for that row. Hence the last row is discarded.

So when constructing queries you would need to be careful to put the more inclusive (less filtered) aggregation first.

Ideally a `full join` should be used, which is symmetrical and the last row is no longer discarded:

Left          | Right         | Combined Result
--------------|---------------|----------------
2             | 3             | (2, 3)
8             | _missing_     | (8, null)
_missing_     | 5             | **(null, 5)**

We can't change this now without potentially changing the results of existing queries, so there is an optional `fullJoins` flag that can be set to `true` in a query.

It is recommended that this be switched on in any query that has multiple aggregations (it has no effect on single-aggregation queries).

It is likely that in a future major version it will become the default.
