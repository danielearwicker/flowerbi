import moment from "moment";

async function allocDb() {
    const publicUrl = (window as any).sitePublicUrl;

    const SQL = await initSqlJs({
        locateFile(f) {
            return `${publicUrl}/${f}`;
        }
    });

    const db = new SQL.Database();
    setupDb(db);
    return db;
}

let db: ReturnType<typeof allocDb> | undefined;

type Await<T> = T extends {
    then(onfulfilled?: (value: infer U) => unknown): unknown;
} ? U : T;

export type Database = NonNullable<Await<typeof db>>;

export function getDb(): Promise<Database> {
    if (!db) {
        db = allocDb();
    }
    return db;
}

function makeRandomDate() {
    const d = new Date(2018, 0, 1);
    d.setDate(d.getDate() + Math.floor(Math.random() * 800));
    return d;
}

function formatDate(d: Date) {
    return moment(d).format("YYYY-MM-DD");
}

function startOfQuarter(d: Date) {
    const m = Math.floor(d.getMonth() / 4) * 4;
    return formatDate(new Date(d.getFullYear(), m, d.getDate()));
}

function startOfMonth(d: Date) {    
    return formatDate(new Date(d.getFullYear(), d.getMonth(), 1));
}

function pick(count: number, base: number) {
     return Math.min(count - 1, Math.floor(Math.random() * count)) + base;     
}

function setupDb(db: Database) {

    const workflowStates = [
        "Ignored",
        "Prioritised",
        "Assigned",
        "Fixed",
        "AsDesigned"
    ];

    const sourcesOfError = [
        "Design flaw",
        "Hackers",
        "Honest mistake"
    ];

    const dates: Date[] = [];
    for (let n = 0; n < 50; n++) {
        dates.push(makeRandomDate());
    }

    const dateRows = dates.map(x => `
        ('${formatDate(x)}', ${x.getFullYear()}, '${startOfQuarter(x)}', '${startOfMonth(x)}')`).join(",");

    const workflows: string[] = [];

    for (const workflowState of workflowStates) {
        for (const sourceOfError of sourcesOfError) {
            for (const resolved of [0, 1]) {
                for (const fixedByCustomer of [0, 1]) {
                    workflows.push(`
                        (${workflows.length + 1}, ${resolved}, '${workflowState}', '${sourceOfError}', ${fixedByCustomer})`);
                }
            }
        }
    }

    const workflowRows = workflows.join(",");

    const categoryCombinations: string[] = [];

    for (const a of [0, 1]) {
        for (const b of [0, 1]) {
            for (const c of [0, 1]) {
                for (const d of [0, 1]) {
                    for (const e of [0, 1]) {
                        for (const f of [0, 1]) {
                            categoryCombinations.push(`
                                (${categoryCombinations.length + 1}, ${a}, ${b}, ${c}, ${d}, ${e}, ${f})`);
                        }
                    }
                }
            }
        }
    }

    const categoryCombinationsRows = categoryCombinations.join(",");

    const bugs: string[] = [];

    for (var n = 0; n < 100; n++) {
        const workflow = pick(workflows.length, 1);
        const customer = pick(6, 1);
        const reported = formatDate(dates[pick(dates.length, 0)]);
        const resolved = formatDate(dates[pick(dates.length, 0)]);
        const assigned = formatDate(dates[pick(dates.length, 0)]);
        const catComb = pick(categoryCombinations.length, 1);
        const coderAssigned = pick(6, 1);
        const coderResolved = pick(6, 1);

        bugs.push(`
            (${n + 1}, ${workflow}, ${customer}, '${reported}', '${resolved}', '${assigned}', ${catComb}, ${coderAssigned}, ${coderResolved})`);
    }

    const bugRows = bugs.join(",");

    const initSql = `
        create table \`Date\` (
            Id date,
            CalendarYearNumber smallint,
            FirstDayOfQuarter date,
            FirstDayOfMonth date
        );

        insert into \`Date\` values ${dateRows};

        create table \`Workflow\` (
            Id int,
            Resolved bit,
            WorkflowState varchar(30),
            SourceOfError varchar(30),
            FixedByCustomer bit
        );

        insert into \`Workflow\` values ${workflowRows};

        create table \`Category\` (
            Id int,
            Label varchar(30)
        );

        insert into \`Category\` values
            (1, 'Crashed'),
            (2, 'Data Loss'),
            (3, 'Security Breach'),
            (4, 'Off By One'),
            (5, 'Slow'),
            (6, 'StackOverflow');

        create table \`Coder\` (
            Id int,
            FullName varchar(30)
        );
    
        insert into \`Coder\` values
                (1, 'Sam'),
                (2, 'Alex'),
                (3, 'Drew'),
                (4, 'Taylor'),
                (5, 'Parker'),
                (6, 'Austin');

        create table \`Customer\` (
            Id int,
            CustomerName varchar(30)
        );
    
        insert into \`Customer\` values
                (1, 'Pies LLC'),
                (2, 'Buns, Inc.'),
                (3, 'Hats-R-Us'),
                (4, 'Silence'),
                (5, 'Egypt'),
                (6, 'Affordability');

        create table \`CategoryCombination\` (
            Id int,
            Crashed bit,
            DataLoss bit,
            SecurityBreach bit,
            OffByOne bit,
            Slow bit,
            StackOverflow bit
        );

        insert into \`CategoryCombination\` values ${categoryCombinationsRows};

        create table \`Bug\` (
            Id int,
            WorkflowId int,
            CustomerId int,
            ReportedDate date,
            ResolvedDate date,
            AssignedDate date,
            CategoryCombinationId int,
            AssignedCoderId int,
            ResolvedCoderId int
        );

        insert into \`Bug\` values ${bugRows};
    `;

    db.run(initSql);
}
