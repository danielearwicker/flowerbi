export type YamlColumn = [type: string] | [type: string, name: string];

export interface YamlColumnSet {
    [column: string]: YamlColumn;
}

export interface YamlTable {
    name?: string;
    id?: YamlColumnSet;
    columns?: YamlColumnSet;
    extends?: string;
    conjoint?: boolean;
}

export interface YamlSchema {
    schema: string;
    name?: string;
    tables: {
        [table: string]: YamlTable;
    };
}
