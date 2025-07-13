/**
 * A utility class for writing indented text output
 * Equivalent to the C# IndentedWriter utility
 */
export class IndentedWriter {
  private readonly output: string[] = [];
  private readonly indentSize: number;

  constructor(indentSize: number = 4) {
    this.indentSize = indentSize;
  }

  /**
   * Write a line with proper indentation
   */
  writeLine(text: string = ''): void {
    const indent = ' '.repeat(this.indentSize);
    this.output.push(`${indent}${text}`);
  }

  /**
   * Write text without indentation (for continued lines)
   */
  write(text: string): void {
    if (this.output.length === 0) {
      this.output.push(text);
    } else {
      this.output[this.output.length - 1] += text;
    }
  }

  /**
   * Get the complete output as a string
   */
  toString(): string {
    return this.output.join('\n');
  }

  /**
   * Clear all output
   */
  clear(): void {
    this.output.length = 0;
  }
}