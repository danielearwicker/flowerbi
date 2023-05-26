"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
const yaml_1 = __importDefault(require("yaml"));
const fs_1 = __importDefault(require("fs"));
const schemaSource = fs_1.default.readFileSync("../dotnet/Demo/FlowerBI.DemoSchema/demoSchema.yaml", "utf-8");
const schema = yaml_1.default.parse(schemaSource);
console.log(schema);
