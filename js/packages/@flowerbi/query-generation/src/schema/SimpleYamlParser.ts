/**
 * Simple YAML parser for FlowerBI schema format
 * Supports only the subset of YAML we need - no generators required
 * Rewritten for clearer indentation handling
 */
export class SimpleYamlParser {
  static parse(yamlText: string): any {
    const lines = yamlText.split('\n');
    const result: any = {};
    
    // Build a stack of { obj, indent } pairs
    const contextStack: Array<{ obj: any; indent: number }> = [{ obj: result, indent: -1 }];

    for (let i = 0; i < lines.length; i++) {
      const line = lines[i];
      const trimmed = line.trim();
      
      // Skip empty lines and comments
      if (!trimmed || trimmed.startsWith('#')) {
        continue;
      }

      const indent = line.length - line.trimStart().length;

      // Pop contexts until we find the right parent level
      while (contextStack.length > 1 && contextStack[contextStack.length - 1].indent >= indent) {
        contextStack.pop();
      }

      const currentContext = contextStack[contextStack.length - 1].obj;

      if (trimmed.includes(':')) {
        const colonIndex = trimmed.indexOf(':');
        const key = trimmed.substring(0, colonIndex).trim();
        const value = trimmed.substring(colonIndex + 1).trim();

        if (value) {
          // Key-value pair on same line
          currentContext[key] = this.parseValue(value);
        } else {
          // Key with nested object
          const nestedObj = {};
          currentContext[key] = nestedObj;
          contextStack.push({ obj: nestedObj, indent });
        }
      } else if (trimmed.startsWith('-')) {
        // Array item
        const value = trimmed.substring(1).trim();
        
        // Ensure current is an array
        if (!Array.isArray(currentContext)) {
          throw new Error('Invalid YAML: array item without array context');
        }
        
        if (value.includes(':')) {
          // Object in array
          const obj = {};
          const colonIndex = value.indexOf(':');
          const key = value.substring(0, colonIndex).trim();
          const val = value.substring(colonIndex + 1).trim();
          (obj as any)[key] = this.parseValue(val);
          currentContext.push(obj);
        } else {
          currentContext.push(this.parseValue(value));
        }
      }
    }

    return result;
  }

  /**
   * Detect indentation levels by analyzing all non-empty, non-comment lines
   * Returns an array of indent amounts in ascending order
   */
  private static detectIndentationLevels(lines: string[]): number[] {
    const indentSet = new Set<number>();
    
    for (const line of lines) {
      const trimmed = line.trim();
      
      // Skip empty lines and comments
      if (!trimmed || trimmed.startsWith('#')) {
        continue;
      }

      const indent = line.length - line.trimStart().length;
      indentSet.add(indent);
    }

    // Convert to sorted array
    const levels = Array.from(indentSet).sort((a, b) => a - b);
    
    // Ensure we have at least level 0
    if (levels.length === 0 || levels[0] !== 0) {
      levels.unshift(0);
    }

    return levels;
  }

  /**
   * Get the indentation level for a given indent amount
   * Returns the index in the indentation levels array
   */
  private static getIndentationLevel(indent: number, indentationLevels: number[]): number {
    const levelIndex = indentationLevels.indexOf(indent);
    
    if (levelIndex !== -1) {
      return levelIndex;
    }

    // If exact match not found, find the closest level that's <= indent
    let closestLevel = 0;
    for (let i = 0; i < indentationLevels.length; i++) {
      if (indentationLevels[i] <= indent) {
        closestLevel = i;
      } else {
        break;
      }
    }

    return closestLevel;
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