declare module 'yamljs/dist/yaml.js' {
  export function parse(str: string): any;
  export function stringify(value: any, inline?: number, indent?: number): string;
  export function load(str: string): any;
  export function dump(value: any): string;
}