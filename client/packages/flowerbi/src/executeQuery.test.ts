import { expandQueryResult, jsonifyQuery, QueryResultJson } from "./executeQuery";
import { IntegerQueryColumn, QueryColumn } from "./QueryColumn";
import { Query, QueryCalculations, QuerySelect } from "./queryModel";

export const Customer = {
    Id: new IntegerQueryColumn<number>("Customer.Id"),
    CustomerName: new QueryColumn<string>("Customer.CustomerName"),
};

export const Bug = {
    Id: new QueryColumn<number>("Bug.Id"),
    CustomerId: new QueryColumn<number>("Bug.CustomerId"),
    Fixed: new QueryColumn<boolean>("Bug.Fixed"),
};

test("jsonifies mixed columns", () => {
    expect(
        jsonifyQuery({
            select: {
                customer: Customer.CustomerName,
                bugCount: Bug.Id.count(),
            },
        })
    ).toStrictEqual({
        select: ["Customer.CustomerName"],
        aggregations: [
            {
                column: "Bug.Id",
                function: "Count",
                filters: undefined,
            },
        ],
        calculations: undefined,
        filters: [],
        orderBy: [],
        skip: 0,
        take: 100,
        totals: false,
        allowDuplicates: undefined,
        comment: undefined,
    });
});

test("jsonifies select", () => {
    expect(
        jsonifyQuery({
            select: {
                customer: Customer.CustomerName,
            },
        })
    ).toStrictEqual({
        select: ["Customer.CustomerName"],
        aggregations: [],
        calculations: undefined,
        filters: [],
        orderBy: [],
        skip: 0,
        take: 100,
        totals: false,
        allowDuplicates: undefined,
        comment: undefined,
    });
});

test("jsonifies params", () => {
    expect(
        jsonifyQuery({
            select: {},
            skip: 3,
            take: 42,
            totals: true,
            allowDuplicates: true,
            comment: "a comment",
        })
    ).toStrictEqual({
        select: [],
        aggregations: [],
        calculations: undefined,
        filters: [],
        orderBy: [],
        skip: 3,
        take: 42,
        totals: true,
        allowDuplicates: true,
        comment: "a comment",
    });
});

test("jsonifies filters", () => {
    expect(
        jsonifyQuery({
            select: {
                bugCount: Bug.Id.count([Customer.CustomerName.lessThan("z")]),
            },
            filters: [Customer.CustomerName.greaterThan("a"), Customer.Id.bitsIn(4 | 8, [0, 4])],
        })
    ).toStrictEqual({
        select: [],
        aggregations: [
            {
                column: "Bug.Id",
                function: "Count",
                filters: [
                    {
                        column: "Customer.CustomerName",
                        operator: "<",
                        value: "z",
                    },
                ],
            },
        ],
        calculations: undefined,
        filters: [
            {
                column: "Customer.CustomerName",
                operator: ">",
                value: "a",
            },
            {
                column: "Customer.Id",
                operator: "BITS IN",
                constant: 12,
                value: [0, 4],
            },
        ],
        orderBy: [],
        skip: 0,
        take: 100,
        totals: false,
        allowDuplicates: undefined,
        comment: undefined,
    });
});

test("jsonifies orderBy", () => {
    expect(
        jsonifyQuery({
            select: {},
            orderBy: [Customer.CustomerName.descending()],
        })
    ).toStrictEqual({
        select: [],
        aggregations: [],
        calculations: undefined,
        filters: [],
        orderBy: [
            {
                column: "Customer.CustomerName",
                descending: true,
            },
        ],
        skip: 0,
        take: 100,
        totals: false,
        allowDuplicates: undefined,
        comment: undefined,
    });
});

test("jsonifies calculations and ordering by keys", () => {
    expect(
        jsonifyQuery({
            select: {
                customer: Customer.CustomerName,
                bugCount: Bug.Id.count(),
                bugsFixed: Bug.Id.count([Bug.Fixed.equalTo(true)]),
            },
            calculations: {
                successRate: [100, "*", ["bugsFixed", "/", "bugCount"]],
                failureRate: [100, "-", [100, "*", ["bugsFixed", "/", "bugCount"]]],
            },
            orderBy: [
                { select: "customer" },
                { select: "bugsFixed", descending: true },
                { calculation: "successRate" },
                { calculation: "failureRate", descending: true },
            ],
        })
    ).toStrictEqual({
        select: ["Customer.CustomerName"],
        aggregations: [
            {
                column: "Bug.Id",
                function: "Count",
                filters: undefined,
            },
            {
                column: "Bug.Id",
                function: "Count",
                filters: [
                    {
                        column: "Bug.Fixed",
                        operator: "=",
                        value: true,
                    },
                ],
            },
        ],
        calculations: [
            {
                first: { value: 100 },
                operator: "*",
                second: {
                    first: { aggregation: 1 },
                    operator: "/",
                    second: { aggregation: 0 },
                },
            },
            {
                first: { value: 100 },
                operator: "-",
                second: {
                    first: { value: 100 },
                    operator: "*",
                    second: {
                        first: { aggregation: 1 },
                        operator: "/",
                        second: { aggregation: 0 },
                    },
                },
            },
        ],
        filters: [],
        orderBy: [
            { type: "Select", index: 0, descending: undefined },
            { type: "Value", index: 1, descending: true },
            { type: "Calculation", index: 0, descending: undefined },
            { type: "Calculation", index: 1, descending: true },
        ],
        skip: 0,
        take: 100,
        totals: false,
        allowDuplicates: undefined,
        comment: undefined,
    });
});

test("passes through JSON orderBy", () => {
    expect(
        jsonifyQuery({
            select: {},
            orderBy: [Customer.CustomerName.descending()],
        })
    ).toStrictEqual({
        select: [],
        aggregations: [],
        calculations: undefined,
        filters: [],
        orderBy: [
            {
                column: "Customer.CustomerName",
                descending: true,
            },
        ],
        skip: 0,
        take: 100,
        totals: false,
        allowDuplicates: undefined,
        comment: undefined,
    });
});

function formatResultsFromQuery<S extends QuerySelect, C extends QueryCalculations<S>>(query: Query<S, C>, result: QueryResultJson) {
    return expandQueryResult<S, C>(query.select, result, query.calculations);
}

test("generates typed results", () => {
    const result = formatResultsFromQuery(
        {
            select: {
                customer: Customer.CustomerName,
                bugCount: Bug.Id.count(),
                bugsFixed: Bug.Id.count([Bug.Fixed.equalTo(true)]),
            },
            calculations: {
                successRate: [100, "*", ["bugsFixed", "/", "bugCount"]],
                failureRate: [100, "-", [100, "*", ["bugsFixed", "/", "bugCount"]]],
            },
            orderBy: [
                { select: "customer" },
                { select: "bugsFixed", descending: true },
                { calculation: "successRate" },
                { calculation: "failureRate", descending: true },
            ],
        },
        {
            records: [
                {
                    selected: ["woolworths"],
                    aggregated: [3, 1, 33.33, 66.67],
                },
            ],
        }
    );

    expect(result).toStrictEqual({
        records: [{ customer: "woolworths", bugCount: 3, bugsFixed: 1, successRate: 33.33, failureRate: 66.67 }],
        totals: undefined,
    });
});

test("generates typed results with totals", () => {
    const result = formatResultsFromQuery(
        {
            select: {
                customer: Customer.CustomerName,
                bugCount: Bug.Id.count(),
                bugsFixed: Bug.Id.count([Bug.Fixed.equalTo(true)]),
            },
            calculations: {
                successRate: [100, "*", ["bugsFixed", "/", "bugCount"]],
                failureRate: [100, "-", [100, "*", ["bugsFixed", "/", "bugCount"]]],
            },
            orderBy: [
                { select: "customer" },
                { select: "bugsFixed", descending: true },
                { calculation: "successRate" },
                { calculation: "failureRate", descending: true },
            ],
        },
        {
            records: [
                {
                    selected: ["woolworths"],
                    aggregated: [3, 1, 33.33, 66.67],
                },
            ],
            totals: {
                selected: [""],
                aggregated: [4, 2, 50, 50],
            },
        }
    );

    expect(result).toStrictEqual({
        records: [{ customer: "woolworths", bugCount: 3, bugsFixed: 1, successRate: 33.33, failureRate: 66.67 }],
        totals: { bugCount: 4, bugsFixed: 2, successRate: 50, failureRate: 50 },
    });
});
