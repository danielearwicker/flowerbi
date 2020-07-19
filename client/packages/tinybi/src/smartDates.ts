import moment, { Moment } from "moment";

function detectDateType(dates: Moment[]): {
    format: string;
    unit: "days" | "months" | "years",
    incr: number;
} {
    
    if (!dates.every(x => x.date() === 1)) {
        return { format: "ll", unit: "days", incr: 1 };
    }

    if (!dates.every(x => x.month() % 3 === 0)) {
        return { format: "MMM YYYY", unit: "months", incr: 1 };
    }

    if (!dates.every(x => x.month() === 0)) {
        return { format: "[Q]Q YYYY", unit: "months", incr: 3 };
    }

    return { format: "YYYY", unit: "years", incr: 1 };
}

export function smartDates<T, R>(
    records: T[],
    getDate: (record: T) => Date | string | number,
    mapTo: (dateText: string, record: T | undefined) => R
) {
    records = [...records];
    records.sort((x, y) => moment(getDate(x)).diff(moment(getDate(y))));

    const { format, unit, incr } = detectDateType(records.map(d => moment(getDate(d))));

    const results: R[] = [];
    let latest: Moment | undefined = undefined;

    for (const record of records) {
        const current = moment(getDate(record));

        if (latest) {
            for (;;) {
                latest = latest.add(incr, unit);
                if (latest >= current) {
                    break;
                }

                results.push(mapTo(latest.format(format), undefined));                
            }
        }

        results.push(mapTo(current.format(format), record));
        latest = current;
    }
    
    return results;
}
