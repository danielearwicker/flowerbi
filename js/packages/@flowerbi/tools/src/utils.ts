import * as fs from 'fs';
import { TypeScriptGenerator, CSharpGenerator } from '@flowerbi/query-generation';

/**
 * Utility functions for programmatic code generation
 */

export interface GenerationResult {
  code: string;
  console: string;
  success: boolean;
  error?: string;
}

/**
 * Generate TypeScript code from YAML file
 */
export function generateTypeScriptFromFile(yamlFilePath: string): GenerationResult {
  try {
    const yamlText = fs.readFileSync(yamlFilePath, 'utf8');
    const result = TypeScriptGenerator.fromYaml(yamlText);
    
    return {
      code: result.code,
      console: result.console,
      success: true
    };
  } catch (error) {
    return {
      code: '',
      console: '',
      success: false,
      error: error instanceof Error ? error.message : String(error)
    };
  }
}

/**
 * Generate C# code from YAML file
 */
export function generateCSharpFromFile(yamlFilePath: string, namespace: string): GenerationResult {
  try {
    const yamlText = fs.readFileSync(yamlFilePath, 'utf8');
    const result = CSharpGenerator.fromYaml(yamlText, namespace);
    
    return {
      code: result.code,
      console: result.console,
      success: true
    };
  } catch (error) {
    return {
      code: '',
      console: '',
      success: false,
      error: error instanceof Error ? error.message : String(error)
    };
  }
}

/**
 * Generate TypeScript code from YAML text
 */
export function generateTypeScriptFromText(yamlText: string): GenerationResult {
  try {
    const result = TypeScriptGenerator.fromYaml(yamlText);
    
    return {
      code: result.code,
      console: result.console,
      success: true
    };
  } catch (error) {
    return {
      code: '',
      console: '',
      success: false,
      error: error instanceof Error ? error.message : String(error)
    };
  }
}

/**
 * Generate C# code from YAML text
 */
export function generateCSharpFromText(yamlText: string, namespace: string): GenerationResult {
  try {
    const result = CSharpGenerator.fromYaml(yamlText, namespace);
    
    return {
      code: result.code,
      console: result.console,
      success: true
    };
  } catch (error) {
    return {
      code: '',
      console: '',
      success: false,
      error: error instanceof Error ? error.message : String(error)
    };
  }
}

/**
 * Write generated code to file only if it's different (equivalent to WriteIfDifferent)
 */
export function writeIfDifferent(filePath: string, content: string): boolean {
  try {
    // Check if file exists and has same content
    if (fs.existsSync(filePath)) {
      const existingContent = fs.readFileSync(filePath, 'utf8');
      if (existingContent === content) {
        return false; // No change needed
      }
    }
    
    // Write the new content
    fs.writeFileSync(filePath, content);
    return true; // File was written
  } catch (error) {
    throw new Error(`Failed to write file ${filePath}: ${error}`);
  }
}