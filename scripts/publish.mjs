#!/usr/bin/env node
// NuGet release steps for DesktopAccountingAPI.QuickBooksDesktop. No dependencies beyond Node.js.
//
//   node scripts/publish.mjs verify-tag [--tag v0.1.0]   tag (or GITHUB_REF_NAME) must equal the csproj <Version>
//   node scripts/publish.mjs pack                        dotnet pack into artifacts/release and inspect the package
//   node scripts/publish.mjs exists                      is this version on nuget.org? (writes published=true|false to GITHUB_OUTPUT)
//   node scripts/publish.mjs push [--dry-run]            upload .nupkg and .snupkg (API key from NUGET_API_KEY), skipping duplicates
//   node scripts/publish.mjs wait [--timeout-minutes 45] poll nuget.org until the version resolves
//   node scripts/publish.mjs smoke [--source <dir>]      install the version into a temporary console app and run it
//   node scripts/publish.mjs release [--dry-run]         all of the above in order
//
// --dry-run builds and packs, shows what would be uploaded, runs the smoke test against the local
// package and makes no network writes. The API key is read from the environment and sent only in the
// X-NuGet-ApiKey header of the upload request; it never appears on a command line or in output.

import { spawnSync } from "node:child_process";
import { appendFileSync, mkdtempSync, readFileSync, readdirSync, rmSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { join, resolve } from "node:path";

const PACKAGE_ID = "DesktopAccountingAPI.QuickBooksDesktop";
const LOWER_ID = PACKAGE_ID.toLowerCase();
const SERVICE_INDEX = "https://api.nuget.org/v3/index.json";
const FLAT = `https://api.nuget.org/v3-flatcontainer/${LOWER_ID}/index.json`;
const OUT = "artifacts/release";
// Conformance test key (valid format and checksum; it authorizes nothing) and the Standard Webhooks reference vector.
const TEST_KEY = "sk_test_Conformance0Key0For0SDK0Tests000010nQFLR";

const args = process.argv.slice(2);
const command = args[0];
const flag = (name) => args.includes(`--${name}`);
const option = (name, fallback) => {
  const i = args.indexOf(`--${name}`);
  return i >= 0 && args[i + 1] !== undefined ? args[i + 1] : fallback;
};
const dryRun = flag("dry-run");

function version() {
  const csproj = readFileSync("src/DesktopAccountingApi.QuickBooksDesktop/DesktopAccountingApi.QuickBooksDesktop.csproj", "utf8");
  const v = /<Version>([^<]+)<\/Version>/.exec(csproj)?.[1];
  if (!v) throw new Error("no <Version> in the csproj");
  return v;
}

function run(cmd, cmdArgs, opts = {}) {
  console.log(`+ ${cmd} ${cmdArgs.join(" ")}`);
  const r = spawnSync(cmd, cmdArgs, { stdio: "inherit", shell: process.platform === "win32", ...opts });
  if (r.status !== 0) throw new Error(`${cmd} exited with ${r.status}`);
}

function verifyTag() {
  const v = version();
  const tag = option("tag", process.env.RELEASE_TAG || process.env.GITHUB_REF_NAME || "");
  if (!tag) {
    if (dryRun) {
      console.log(`no tag given; would require v${v}`);
      return;
    }
    throw new Error("no tag: pass --tag v<version> or set RELEASE_TAG");
  }
  if (tag !== `v${v}`) throw new Error(`tag ${tag} does not match the package version ${v} (expected v${v})`);
  console.log(`tag ${tag} matches version ${v}`);
}

function pack() {
  rmSync(OUT, { recursive: true, force: true });
  run("dotnet", ["pack", "src/DesktopAccountingApi.QuickBooksDesktop", "-c", "Release", "-o", OUT]);
  run("node", ["scripts/verify-package.mjs", OUT]);
}

function artifacts() {
  const v = version();
  const files = readdirSync(OUT);
  const nupkg = `${PACKAGE_ID}.${v}.nupkg`;
  const snupkg = `${PACKAGE_ID}.${v}.snupkg`;
  if (!files.includes(nupkg) || !files.includes(snupkg)) throw new Error(`${OUT} lacks ${nupkg} or ${snupkg}; run pack first`);
  return { nupkg: join(OUT, nupkg), snupkg: join(OUT, snupkg) };
}

async function published() {
  const v = version();
  const res = await fetch(FLAT, { headers: { "Cache-Control": "no-cache" } });
  if (res.status === 404) return false;
  if (!res.ok) throw new Error(`GET ${FLAT}: HTTP ${res.status}`);
  const body = await res.json();
  return (body.versions ?? []).includes(v.toLowerCase());
}

async function exists() {
  const yes = await published();
  console.log(`${PACKAGE_ID} ${version()} is ${yes ? "already" : "not yet"} on nuget.org`);
  if (process.env.GITHUB_OUTPUT) appendFileSync(process.env.GITHUB_OUTPUT, `published=${yes}\n`);
  return yes;
}

async function endpoints() {
  const res = await fetch(SERVICE_INDEX);
  if (!res.ok) throw new Error(`GET ${SERVICE_INDEX}: HTTP ${res.status}`);
  const index = await res.json();
  const find = (prefix) => index.resources.find((r) => r["@type"].startsWith(prefix))?.["@id"];
  const pkg = find("PackagePublish/");
  const symbols = find("SymbolPackagePublish/");
  if (!pkg || !symbols) throw new Error("nuget.org service index lacks PackagePublish or SymbolPackagePublish");
  return { pkg, symbols };
}

async function upload(url, file, apiKey) {
  const form = new FormData();
  form.append("package", new Blob([readFileSync(file)]), "package.nupkg");
  const res = await fetch(url, { method: "PUT", headers: { "X-NuGet-ApiKey": apiKey, "X-NuGet-Protocol-Version": "4.1.0" }, body: form });
  const text = await res.text();
  if (res.status === 409) {
    console.log(`${file}: this version already exists on nuget.org; skipped`);
    return;
  }
  if (res.status !== 201 && res.status !== 202 && res.status !== 200) {
    throw new Error(`upload of ${file} failed: HTTP ${res.status} ${text.slice(0, 500)}`);
  }
  console.log(`${file}: uploaded (HTTP ${res.status})`);
}

async function push() {
  const { nupkg, snupkg } = artifacts();
  const { pkg, symbols } = await endpoints();
  if (dryRun) {
    console.log(`dry run: would PUT ${nupkg} to ${pkg}`);
    console.log(`dry run: would PUT ${snupkg} to ${symbols}`);
    return;
  }
  if (await published()) {
    console.log(`${PACKAGE_ID} ${version()} is already on nuget.org; nothing to upload`);
    return;
  }
  const apiKey = process.env.NUGET_API_KEY;
  if (!apiKey) throw new Error("NUGET_API_KEY is not set (the NuGet/login step provides it)");
  await upload(pkg, nupkg, apiKey);
  await upload(symbols, snupkg, apiKey);
}

async function wait() {
  if (dryRun) {
    console.log(`dry run: would poll ${FLAT} for ${version()}`);
    return;
  }
  const minutes = Number(option("timeout-minutes", "45"));
  const deadline = Date.now() + minutes * 60_000;
  for (let attempt = 1; ; attempt++) {
    if (await published()) {
      console.log(`${PACKAGE_ID} ${version()} resolves on nuget.org`);
      return;
    }
    if (Date.now() > deadline) throw new Error(`${PACKAGE_ID} ${version()} did not appear on nuget.org within ${minutes} minutes (validation and indexing can take longer; re-run the workflow to resume)`);
    console.log(`waiting for nuget.org to list ${version()} (attempt ${attempt})`);
    await new Promise((r) => setTimeout(r, 30_000));
  }
}

function smoke() {
  const v = version();
  const local = option("source", dryRun ? OUT : undefined);
  const dir = mkdtempSync(join(tmpdir(), "daapi-dotnet-smoke-"));
  try {
    const sources = [local ? `<add key="local" value="${resolve(local)}" />` : "", `<add key="nuget.org" value="${SERVICE_INDEX}" protocolVersion="3" />`].join("\n    ");
    writeFileSync(join(dir, "nuget.config"), `<?xml version="1.0" encoding="utf-8"?>\n<configuration>\n  <packageSources>\n    <clear />\n    ${sources}\n  </packageSources>\n</configuration>\n`);
    writeFileSync(join(dir, "Smoke.csproj"), `<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RestorePackagesPath>packages</RestorePackagesPath>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="${PACKAGE_ID}" Version="[${v}]" />
  </ItemGroup>
</Project>
`);
    writeFileSync(join(dir, "Program.cs"), `using DesktopAccountingApi.QuickBooksDesktop;
using DesktopAccountingApi.QuickBooksDesktop.Models;

if (DesktopAccountingApiClient.SdkVersion != "${v}") throw new Exception("SdkVersion is " + DesktopAccountingApiClient.SdkVersion);
using var client = new DesktopAccountingApiClient(new ClientOptions { ApiKey = "${TEST_KEY}", EndUserId = "eu_smoke" });
var headers = new Dictionary<string, string>
{
    ["webhook-id"] = "msg_p5jXN8AQM9LWM0D4loKWxJek",
    ["webhook-timestamp"] = "1614265330",
    ["webhook-signature"] = "v1,g0hM9SsE+OTPJTGt/tmIKtSyZlE3uFJELVlNIOLJ1OE=",
};
WebhookVerifier.VerifySignature("{\\"test\\": 2432232314}", headers, "whsec_MfKQ9r8GKYqrTwjUPD8ILPZIo2LaLaSw", new WebhookVerifyOptions { Clock = () => DateTimeOffset.FromUnixTimeSeconds(1614265330) });
var input = new InvoiceUpdateInput { RevisionNumber = "1", Memo = null };
var json = DesktopAccountingApiJson.Serialize(input);
if (json != "{\\"revisionNumber\\":\\"1\\",\\"memo\\":null}") throw new Exception(json);
Console.WriteLine($"smoke OK: {ApiInfo()}");
static string ApiInfo() => $"DesktopAccountingAPI.QuickBooksDesktop {DesktopAccountingApiClient.SdkVersion}, contract {DesktopAccountingApiClient.ContractSha256[..12]}";
`);
    const restore = () => spawnSync("dotnet", ["restore", "--no-cache"], { cwd: dir, stdio: "inherit", shell: process.platform === "win32" });
    // The CDN can lag the flat container index for a few minutes after publishing.
    for (let attempt = 1; ; attempt++) {
      console.log(`+ dotnet restore (attempt ${attempt})`);
      if (restore().status === 0) break;
      if (attempt >= (local ? 1 : 10)) throw new Error("restore of the published package failed");
      spawnSync(process.execPath, ["-e", "setTimeout(() => {}, 30000)"]);
    }
    run("dotnet", ["run", "--no-restore"], { cwd: dir });
  } finally {
    rmSync(dir, { recursive: true, force: true });
  }
}

async function main() {
  switch (command) {
    case "version":
      console.log(version());
      break;
    case "verify-tag":
      verifyTag();
      break;
    case "pack":
      pack();
      break;
    case "exists":
      await exists();
      break;
    case "push":
      await push();
      break;
    case "wait":
      await wait();
      break;
    case "smoke":
      smoke();
      break;
    case "release":
      verifyTag();
      pack();
      if (dryRun) {
        await push();
        await wait();
        smoke();
        console.log("dry run complete: nothing was uploaded");
        break;
      }
      await push();
      await wait();
      smoke();
      break;
    default:
      console.error("usage: node scripts/publish.mjs <version|verify-tag|pack|exists|push|wait|smoke|release> [--dry-run] [--tag vX.Y.Z] [--source dir]");
      process.exit(2);
  }
}

main().catch((err) => {
  console.error(`publish: ${err.message}`);
  process.exit(1);
});
