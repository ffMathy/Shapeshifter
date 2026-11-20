import { TargetApp, sleep } from "../desktop.ts";
import { xcopy, clipboardTypes, pasteFromHistory } from "./capture-driver.ts";
import { readFileSync, writeFileSync } from "node:fs";
const S = process.argv[2];
const app = await TargetApp.start();
try {
  await xcopy(readFileSync(`${S}/img.png`), "image/png");
  await xcopy("filler after image 2");
  await pasteFromHistory(app, ["v"]);
  console.log("types", clipboardTypes());
  const r = Bun.spawnSync(["timeout", "5", "xclip", "-selection", "clipboard", "-o", "-t", "image/png"], { env: process.env });
  writeFileSync(`${S}/back.png`, r.stdout);
  console.log("bytes", r.stdout.length);
} finally { await app.stop(); }
