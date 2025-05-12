import moment, { Moment } from "moment";

export type FillDate = Date | string | number | Moment;

function parseDate(val: FillDate) {
    // Ensure a numeric year is not interpreted as offset from 1970!
    if (typeof val === "number") {
        val = "" + val;
    }

    return moment(val);
}

/**
 * Three operations on dates used by {@link fillDates}.
 */
export type FillDateType = {
    /** Round the given date down to the nearest whole unit (e.g. start of month, quarter, year) */
    round(d: Moment): Moment;
    /** Format the given date to a string */
    format(d: Moment): string;
    /** Increment the date by the unit. The given date will already be rounded down. */
    increment(d: Moment): Moment;
};

const days: FillDateType = {
    round(d) {
        return d.clone().startOf("day");
    },
    format(d) {
        return d.format("ll");
    },
    increment(d) {
        return d.clone().add(1, "day");
    },
};

const months: FillDateType = {
    round(d) {
        return d.clone().startOf("month");
    },
    format(d) {
        return d.format("MMM YYYY");
    },
    increment(d) {
        return d.clone().add(1, "month");
    },
};

const quarters: FillDateType = {
    round(d) {
        return d.clone().startOf("quarter");
    },
    format(d) {
        const monthFirst = d.format("MMM");
        const monthLast = d.clone().add(2, "months").format("MMM");
        const year = d.format("YYYY");
        return `${monthFirst}-${monthLast} ${year}`;
    },
    increment(d) {
        return d.clone().add(3, "months");
    },
};

const years: FillDateType = {
    round(d) {
        return d.clone().startOf("year");
    },
    format(d) {
        return d.format("YYYY");
    },
    increment(d) {
        return d.clone().add(1, "year");
    },
};

/**
 * Standard built-in date types. To customise, implement the {@link FillDateType} interface.
 */
export const dateTypes = {
    days,
    months,
    quarters,
    years,
} as const;

/**
 * Examines a set of dates and chooses the most specific type that includes them all. If
 * all fall on Jan, 1 then `years` is chosen, and so on.
 */
export function detectDateType(dates: Moment[]): FillDateType {
    return !dates.every((x) => x.date() === 1)
        ? dateTypes.days
        : !dates.every((x) => x.month() % 3 === 0)
        ? dateTypes.months
        : !dates.every((x) => x.month() === 0)
        ? dateTypes.quarters
        : dateTypes.years;
}

/** Options for {@link fillDates} function. */
export interface FillDatesOptions<T, R> {
    /** The records to base the filled list on. */
    records: T[];
    /** The operations to use for rounding, incrementing and formatting dates. */
    type?: FillDateType;
    /** Extracts a date value from a record in the input list. */
    getDate(record: T): FillDate;
    /**
     * Generate a record for a date, from the formatted {@link dateText} and
     * the input record for that date, if any.
     */
    fill(dateText: string, record: T | undefined): R;
    /**
     * The minimum date to generate. It will be rounded down by the {@link type}
     * so doesn't need to be on an exact boundary.
     */
    min?: FillDate;
    /**
     * The maximum date to generate. It will be rounded down by the {@link type}
     * so doesn't need to be on an exact boundary.
     */
    max?: FillDate;
}

/**
 * When querying for a time series chart, e.g. x-axis is _Month_ and y-axis is
 * _Total Sales_, there may be months where nothing was sold so they are
 * missing from the list of records.
 *
 * To render a proper time-series, we need these gaps to be filled in with
 * runs of fake records that give zero amounts for those months. e.g.
 *
 * ```ts
 * const filled = fillDates({
 *     records: [
 *         { date: '2020-04-01', totalSales: 10 },
 *         { date: '2020-06-01', totalSales: 4 },
 *         { date: '2020-07-01', totalSales: 9 },
 *     ],
 *     type: dateTypes.months,
 *     getDate: rec => rec.date,
 *     fill: (label, rec) => ({
 *         label,
 *         totalSales: 0,
 *         ...rec
 *     })
 * });
 * ```
 *
 * In the above example we add a `label` property to all the records, and
 * for the records that fill the gaps we set the `totalSales` property to 0.
 * For the real records, `...rec` will copy the real value of `totalSales`.
 *
 * To do this, we need to know:
 *
 * - how to round a date to the start of a unit (year, month, quarter),
 * - how to increment a date by that unit,
 * - how to format a date to a string for display.
 *
 * These operations are encapsulated by the {@link FillDateType} interface.
 * Several built-in types are provided in {@link dateTypes}, but you can
 * implement your own.
 *
 * Optionally you can also pass `min` and `max` dates, which will cause
 * extra records to be added at the start and end of the range if necessary.
 *
 * If you don't pass a `type`, a suitable type will be detected based on
 * how the input record dates fall on unit boundaries.
 */
export function fillDates<T, R>({ records, getDate, fill, min, max, type }: FillDatesOptions<T, R>) {
    records = [...records];
    records.sort((x, y) => parseDate(getDate(x)).diff(parseDate(getDate(y))));

    type = type ?? detectDateType(records.map((d) => parseDate(getDate(d))));

    const results: R[] = [];
    let latest: Moment | undefined = undefined;
    const lower = min ? type.round(parseDate(min)) : undefined;

    for (const record of records) {
        const d = getDate(record);
        if (!d) continue;

        const current = parseDate(d);

        for (;;) {
            latest = latest ? type.increment(latest) : lower;
            if (!latest || latest >= current) {
                break;
            }

            results.push(fill(type.format(latest), undefined));
        }

        results.push(fill(type.format(current), record));
        latest = current;
    }

    if (latest && max) {
        const upper = type.round(parseDate(max));
        for (;;) {
            latest = type.increment(latest);
            if (latest > upper) {
                break;
            }

            results.push(fill(type.format(latest), undefined));
        }
    }

    return results;
}

/** @deprecated */
export function smartDates<T, R>(
    records: T[],
    min: FillDate | undefined,
    max: FillDate | undefined,
    getDate: (record: T) => FillDate,
    fill: (dateText: string, record: T | undefined) => R
) {
    return fillDates({ records, min, max, getDate, fill });
}
