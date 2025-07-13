/**
 * Simple YAML parser for FlowerBI schema format
 * Supports only the subset of YAML we need - no generators required
 */
export class SimpleYamlParser {
  static parse(yamlText: string): any {
    const lines = yamlText.split('\n');
    const result: any = {};
    let currentLevel = 0;
    let stack: any[] = [result];
    let currentKey = '';

    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];
      const trimmed = line.trim();
      
      // Skip empty lines and comments
      if (!trimmed || trimmed.startsWith('#')) {
        continue;
      }

      // Calculate indentation level
      const indent = line.length - line.trimStart().length;
      const level = Math.floor(indent / 2);

      // Adjust stack based on indentation
      while (stack.length > level + 1) {
        stack.pop();
      }

      const current = stack[stack.length - 1];

      if (trimmed.includes(':')) {
        const colonIndex = trimmed.indexOf(':');
        const key = trimmed.substring(0, colonIndex).trim();
        const value = trimmed.substring(colonIndex + 1).trim();

        if (value) {
          // Key-value pair on same line
          current[key] = this.parseValue(value);
        } else {
          // Key with nested object
          current[key] = {};
          stack.push(current[key]);
        }
      } else if (trimmed.startsWith('-')) {
        // Array item
        const value = trimmed.substring(1).trim();
        
        // Ensure current is an array
        if (!Array.isArray(current)) {
          throw new Error('Invalid YAML: array item without array context');
        }
        
        if (value.includes(':')) {
          // Object in array
          const obj = {};
          const colonIndex = value.indexOf(':');
          const key = value.substring(0, colonIndex).trim();
          const val = value.substring(colonIndex + 1).trim();
          (obj as any)[key] = this.parseValue(val);
          current.push(obj);
        } else {
          current.push(this.parseValue(value));
        }
      }
    }

    return result;
  }

  private static parseValue(value: string): any {
    // Remove quotes
    if ((value.startsWith('"') && value.endsWith('"')) || 
        (value.startsWith("'") && value.endsWith("'"))) {
      return value.slice(1, -1);
    }

    // Parse array notation [type] or [type, name]
    if (value.startsWith('[') && value.endsWith(']')) {
      const inner = value.slice(1, -1).trim();
      if (!inner) return [];
      
      return inner.split(',').map(item => item.trim());
    }

    // Parse numbers
    if (/^\d+$/.test(value)) {
      return parseInt(value, 10);
    }
    if (/^\d+\.\d+$/.test(value)) {
      return parseFloat(value);
    }

    // Parse booleans
    if (value === 'true') return true;
    if (value === 'false') return false;
    if (value === 'null') return null;

    // Default to string
    return value;
  }
}