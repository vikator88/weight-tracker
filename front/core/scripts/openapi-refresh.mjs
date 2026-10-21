// Downloads the API's OpenAPI document into the committed snapshot. Needs the API running in Development.
import { writeFile } from "node:fs/promises";

const url = process.env.OPENAPI_URL ?? "http://localhost:5199/openapi/v1.json";
const target = new URL("../openapi/v1.json", import.meta.url);

const response = await fetch(url);

if (!response.ok) {
  console.error(`GET ${url} answered ${response.status}`);
  process.exit(1);
}

const document = await response.json();
await writeFile(target, `${JSON.stringify(document, null, 2)}\n`);
console.log(`Snapshot written from ${url}`);
