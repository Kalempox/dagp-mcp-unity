import type { z } from "zod";

type JsonSchemaProp = {
  type: string;
  description?: string;
  enum?: readonly string[];
  default?: unknown;
  items?: JsonSchemaProp;
};

/**
 * Lightweight Zod -> JSON Schema converter.
 * Supports flat object schemas with: string, number, boolean, enum, array, optional, default, nullable.
 * Includes `default` so MCP hosts know the parameter is optional with a sensible value.
 */
export function zodToJsonSchema(schema: z.ZodObject<any>): Record<string, unknown> {
  const shape = (schema as any)._def.shape();
  const properties: Record<string, JsonSchemaProp> = {};
  const required: string[] = [];

  for (const [key, value] of Object.entries(shape)) {
    const { prop, isOptional } = describeProp(value as z.ZodTypeAny);
    properties[key] = prop;
    if (!isOptional) required.push(key);
  }

  return {
    type: "object",
    properties,
    ...(required.length ? { required } : {}),
    additionalProperties: false,
  };
}

function describeProp(node: z.ZodTypeAny): { prop: JsonSchemaProp; isOptional: boolean } {
  let isOptional = false;
  let defaultValue: unknown = undefined;
  let inner: z.ZodTypeAny = node;

  // Recursively unwrap Optional / Default / Nullable in any order until we hit a leaf.
  while (true) {
    const def = (inner as any)._def;
    if (def.typeName === "ZodOptional") { isOptional = true; inner = def.innerType; }
    else if (def.typeName === "ZodDefault") {
      isOptional = true;
      if (defaultValue === undefined) defaultValue = def.defaultValue();
      inner = def.innerType;
    }
    else if (def.typeName === "ZodNullable") { isOptional = true; inner = def.innerType; }
    else break;
  }

  const innerDef = (inner as any)._def;
  let prop: JsonSchemaProp;

  switch (innerDef.typeName) {
    case "ZodString":  prop = { type: "string"  }; break;
    case "ZodNumber":  prop = { type: "number"  }; break;
    case "ZodBoolean": prop = { type: "boolean" }; break;
    case "ZodEnum":    prop = { type: "string", enum: innerDef.values }; break;
    case "ZodArray": {
      const item = describeProp(innerDef.type).prop;
      prop = { type: "array", items: item };
      break;
    }
    case "ZodObject": prop = { type: "object" }; break;
    default:          prop = { type: "string" };
  }

  if (innerDef.description) prop.description = innerDef.description;
  if (defaultValue !== undefined) prop.default = defaultValue;

  return { prop, isOptional };
}
