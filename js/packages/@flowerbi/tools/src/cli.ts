#!/usr/bin/env node

import * as fs from 'fs';
import * as path from 'path';
import { TypeScriptGenerator, CSharpGenerator } from '@flowerbi/query-generation';

/**
 * Command-line interface for FlowerBI code generation tools
 * Equivalent to the C# FlowerBI.Tools Program.cs
 */

function getVersion(): string {
  try {
    const packageJsonPath = path.join(__dirname, '..', 'package.json');
    const packageJson = JSON.parse(fs.readFileSync(packageJsonPath, 'utf8'));
    return packageJson.version || 'unknown';
  } catch {
    return 'unknown';
  }
}

function showUsage(): void {
  console.log('Usage:');
  console.log();
  console.log('Generate TypeScript from a yaml declaration:');
  console.log();
  console.log('        flowerbi-tools ts <yaml-file> <ts-file>');
  console.log();
  console.log('Generate C# from a yaml declaration:');
  console.log();
  console.log('        flowerbi-tools cs <yaml-file> <cs-file> <cs-namespace>');
  console.log();
}

function generateTypeScript(yamlFile: string, tsFile: string): void {
  try {
    // Read YAML file
    const yamlText = fs.readFileSync(yamlFile, 'utf8');
    
    // Generate TypeScript code
    const result = TypeScriptGenerator.fromYaml(yamlText);
    
    // Write console output
    if (result.console) {
      console.log(result.console);
    }
    
    // Write output file
    fs.writeFileSync(tsFile, result.code);
    
  } catch (error) {
    console.error(`Error generating TypeScript: ${error instanceof Error ? error.message : String(error)}`);
    process.exit(1);
  }
}

function generateCSharp(yamlFile: string, csFile: string, csNamespace: string): void {
  try {
    // Read YAML file
    const yamlText = fs.readFileSync(yamlFile, 'utf8');
    
    // Generate C# code
    const result = CSharpGenerator.fromYaml(yamlText, csNamespace);
    
    // Write console output
    if (result.console) {
      console.log(result.console);
    }
    
    // Write output file
    fs.writeFileSync(csFile, result.code);
    
  } catch (error) {
    console.error(`Error generating C#: ${error instanceof Error ? error.message : String(error)}`);
    process.exit(1);
  }
}

function main(): void {
  const args = process.argv.slice(2);
  
  console.log(`FlowerBI Tools ${getVersion()} running on Node.js ${process.version}`);
  
  if (args.length === 3 && args[0] === 'ts') {
    const [, yamlFile, tsFile] = args;
    generateTypeScript(yamlFile, tsFile);
  }
  else if (args.length === 4 && args[0] === 'cs') {
    const [, yamlFile, csFile, csNamespace] = args;
    generateCSharp(yamlFile, csFile, csNamespace);
  }
  else {
    showUsage();
    process.exit(1);
  }
}

// Run the CLI
main();