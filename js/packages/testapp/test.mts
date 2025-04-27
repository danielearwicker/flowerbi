// Named exports are auto-generated on C# build.
import bootsharp, { FlowerBI } from "flowerbi-bootsharp";
import { QueryTypes, Sequelize } from "sequelize";

// Initializing dotnet runtime and invoking entry point.
await bootsharp.boot();

const formatter: FlowerBI.ISqlFormatter = {
    identifier: (name) => name,
    escapedIdentifierPair: (id1, id2) => `${id1}.${id2}`,
    skipAndTake: (skip, take) => `
limit ${take} -- take
offset ${skip} -- skip
`,
    conditional: (predExpr, thenExpr, elseExpr) =>
        `case when ${predExpr} then ${thenExpr} else ${elseExpr} END`,
    castToFloat: (valueExpr) => `cast(${valueExpr} as real)`,
    getParamPrefix: () => ":",
};

// Invoking 'Program.GetBackendName' C# method.
const schema = await FlowerBI.Bootsharp.Program.schema(
    `
schema: TestSchema
name: Testing
tables:

    Department:
        id:
            Id: [long]
        columns:
            DepartmentName: [string]

    Vendor:
        name: Supplier
        id:
            Id: [long]
        columns:
            VendorName: [string]
            DepartmentId: [Department]

    Invoice:
        id:
            Id: [long]
        columns:
            VendorId: [Vendor]
            DepartmentId: [Department]
            Amount: [decimal, FancyAmount]
            Paid: [bool?]

    Tag:
        id:
            Id: [long]
        columns:
            TagName: [string]

    InvoiceTag:
        columns:
            InvoiceId: [Invoice]
            TagId: [Tag]

    Category:
        id:
            Id: [long]
        columns:
            CategoryName: [string]

    InvoiceCategory:
        columns:
            InvoiceId: [Invoice]
            CategoryId: [Category]

    AnnotationName:
        conjoint: true
        id:
            Id: [long]
        columns:
            Name: [string]

    AnnotationValue:
        conjoint: true
        id:
            Id: [long]
        columns:
            AnnotationNameId: [AnnotationName]
            Value: [string]

    InvoiceAnnotation:
        conjoint: true
        associative: [InvoiceId, AnnotationValueId]
        columns:
            InvoiceId: [Invoice]
            AnnotationValueId: [AnnotationValue]
`,
    formatter
);

const sequelize = new Sequelize(
    "postgres://postgres:mysecretpassword@127.0.0.1:5432/postgres"
);

const generated = JSON.parse(
    schema?.generateQuery(
        JSON.stringify({
            Select: ["Vendor.VendorName"],
            Aggregations: [
                {
                    Function: "Sum",
                    Column: "Invoice.Amount",
                },
            ],
            OrderBy: [
                {
                    Type: "Value",
                    Index: 0,
                    Descending: true,
                },
            ],
            Skip: 0,
            Take: 10,
        })
    ) ?? "{}"
);

const x = await sequelize.query(generated.sql, {
    type: QueryTypes.SELECT,
});

console.log(x);
