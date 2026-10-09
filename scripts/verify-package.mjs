#!/usr/bin/env node
// Checks the packed NuGet package before it can be published:
//   node scripts/verify-package.mjs <dir with the .nupkg and .snupkg>
// Both target frameworks with XML docs, README and license metadata, repository metadata,
// netstandard2.0 dependencies, and portable PDBs for both frameworks in the symbols package.
import { readdirSync, readFileSync } from "node:fs";
import { join } from "node:path";
import { readZip } from "./nupkg.mjs";

const PACKAGE_ID = "DesktopAccountingAPI.QuickBooksDesktop";
const ASSEMBLY = "DesktopAccountingApi.QuickBooksDesktop";
const TFMS = ["netstandard2.0", "net8.0"];

const dir = process.argv[2] ?? "artifacts";
const csproj = readFileSync("src/DesktopAccountingApi.QuickBooksDesktop/DesktopAccountingApi.QuickBooksDesktop.csproj", "utf8");
const version = /<Version>([^<]+)<\/Version>/.exec(csproj)?.[1];
if (!version) throw new Error("no <Version> in the csproj");

const errors = [];
const expect = (ok, what) => {
  if (!ok) errors.push(what);
};

const files = readdirSync(dir);
const nupkg = `${PACKAGE_ID}.${version}.nupkg`;
const snupkg = `${PACKAGE_ID}.${version}.snupkg`;
expect(files.includes(nupkg), `${nupkg} is missing in ${dir}`);
expect(files.includes(snupkg), `${snupkg} is missing in ${dir}`);

if (files.includes(nupkg)) {
  const zip = readZip(join(dir, nupkg));
  for (const tfm of TFMS) {
    for (const ext of ["dll", "xml"]) {
      const name = `lib/${tfm}/${ASSEMBLY}.${ext}`;
      expect(zip.has(name), `${nupkg} lacks ${name}`);
    }
    const xml = zip.get(`lib/${tfm}/${ASSEMBLY}.xml`)?.().toString("utf8") ?? "";
    expect(xml.includes("M:DesktopAccountingApi.QuickBooksDesktop.Resources.QbdInvoicesResource.CreateAsync"), `XML docs for ${tfm} lack resource method docs`);
    expect(xml.includes("P:DesktopAccountingApi.QuickBooksDesktop.Models.Invoice.Subtotal"), `XML docs for ${tfm} lack model docs`);
  }
  expect(zip.has("README.md"), `${nupkg} lacks README.md`);
  expect(zip.has("LICENSE"), `${nupkg} lacks LICENSE`);
  // nuget.org shows the package icon; it must be a PNG of at least 128x128.
  const icon = zip.get("icon.png")?.();
  expect(Boolean(icon), `${nupkg} lacks icon.png`);
  if (icon) {
    const png = icon.subarray(0, 8).equals(Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]));
    expect(png && icon.readUInt32BE(16) >= 128 && icon.readUInt32BE(20) >= 128, "icon.png is a PNG of at least 128x128");
  }
  const nuspecName = [...zip.keys()].find((n) => n.endsWith(".nuspec"));
  const nuspec = nuspecName ? zip.get(nuspecName)().toString("utf8") : "";
  expect(nuspec.includes(`<id>${PACKAGE_ID}</id>`), "nuspec id");
  expect(nuspec.includes(`<version>${version}</version>`), "nuspec version");
  expect(nuspec.includes('<license type="expression">MIT</license>'), "nuspec MIT license expression");
  expect(nuspec.includes("<readme>README.md</readme>"), "nuspec readme");
  expect(nuspec.includes("<icon>icon.png</icon>"), "nuspec icon");
  expect(/<repository type="git" url="https:\/\/github\.com\/DesktopAccountingAPI\/quickbooks-desktop-dotnet"/.test(nuspec), "nuspec repository url");
  const ns20 = /<group targetFramework="\.NETStandard2\.0">([\s\S]*?)<\/group>/.exec(nuspec)?.[1] ?? "";
  for (const dep of ["System.Text.Json", "Microsoft.Bcl.AsyncInterfaces", "Portable.System.DateTimeOnly"]) {
    expect(ns20.includes(`id="${dep}"`), `nuspec netstandard2.0 dependency ${dep}`);
  }
  expect(/<group targetFramework="net8\.0" \/>/.test(nuspec), "nuspec net8.0 has no dependencies");
  console.log(`${nupkg}: ${zip.size} entries`);
}

if (files.includes(snupkg)) {
  const zip = readZip(join(dir, snupkg));
  for (const tfm of TFMS) expect(zip.has(`lib/${tfm}/${ASSEMBLY}.pdb`), `${snupkg} lacks lib/${tfm}/${ASSEMBLY}.pdb`);
  console.log(`${snupkg}: ${zip.size} entries`);
}

if (errors.length > 0) {
  for (const e of errors) console.error(`package check failed: ${e}`);
  process.exit(1);
}
console.log(`package ${PACKAGE_ID} ${version} OK (${TFMS.join(", ")}, XML docs, README, icon, MIT license, symbols)`);
