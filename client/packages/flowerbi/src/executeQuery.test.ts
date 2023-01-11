import { jsonifyQuery } from "./executeQuery";
import { QueryColumn } from "./QueryColumn";

export const Customer = {
    Id: new QueryColumn<number>("Customer.Id"),
    CustomerName: new QueryColumn<string>("Customer.CustomerName"),
};

export const Bug = {
    Id: new QueryColumn<number>("Bug.Id"),
    CustomerId: new QueryColumn<number>("Bug.CustomerId"),    
};

test("jsonifies mixed columns", () => {    
    expect(jsonifyQuery({
        select: {
            customer: Customer.CustomerName,
            bugCount: Bug.Id.count()
        }
    })).toStrictEqual({
        select: ["Customer.CustomerName"],
        aggregations: [{
            column: "Bug.Id",
            function: "Count",
            filters: undefined
        }],
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
    expect(jsonifyQuery({
        select: {
            customer: Customer.CustomerName,
        }
    })).toStrictEqual({
        select: ["Customer.CustomerName"],
        aggregations: [],
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
    expect(jsonifyQuery({
        select: {},
        skip: 3,
        take: 42,
        totals: true,
        allowDuplicates: true,
        comment: "a comment",
    })).toStrictEqual({
        select: [],
        aggregations: [],
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
    expect(jsonifyQuery({
        select: {
            bugCount: Bug.Id.count([
                Customer.CustomerName.lessThan("z")
            ])
        },
        filters: [
            Customer.CustomerName.greaterThan("a")
        ]
        
    })).toStrictEqual({
        select: [],
        aggregations: [{
            column: "Bug.Id",
            function: "Count",
            filters: [{
                column: "Customer.CustomerName",
                operator: "<",
                value: "z",
            }]
        }],
        filters: [{
            column: "Customer.CustomerName",
            operator: ">",
            value: "a",
        }],
        orderBy: [],
        skip: 0,
        take: 100,
        totals: false,
        allowDuplicates: undefined,
        comment: undefined,
    });
});

test("jsonifies orderBy", () => {    
    expect(jsonifyQuery({
        select: {},
        orderBy: [
            Customer.CustomerName.descending()
        ]
    })).toStrictEqual({
        select: [],
        aggregations: [],
        filters: [],
        orderBy: [{
            column: "Customer.CustomerName",
            descending: true,
        }],
        skip: 0,
        take: 100,
        totals: false,
        allowDuplicates: undefined,
        comment: undefined,
    });
});
