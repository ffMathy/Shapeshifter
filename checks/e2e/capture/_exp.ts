import { TargetApp, sleep } from "../desktop.ts";
import { xcopy, clipboardTypes, clipboardBytes, pasteFromHistory, watchNotifications, scratch } from "./capture-driver.ts";
import { readFileSync, writeFileSync } from "node:fs";
const S = process.argv[2];
const app = await TargetApp.start();
try {
  // HTML
  await app.send(`copy-html ${Buffer.from("bold words").toString("base64")} ${Buffer.from("<b>bold</b> words").toString("base64")}`);
  await sleep(800);
  console.log("after html copy", clipboardTypes());
  await xcopy("filler after html");
  console.log("pasted:", JSON.stringify(await pasteFromHistory(app, ["v"])));
  console.log("types", clipboardTypes(), "html:", JSON.stringify(clipboardBytes("text/html").toString()));
  // image
  await xcopy(readFileSync(`${S}/img.png`), "image/png");
  await xcopy("filler after image");
  await app.prepare();
  const p = pasteFromHistory(app, ["v"]);
  const seen = watchNotifications(3000);
  console.log("pasted:", JSON.stringify(await p));
  console.log("notifications after image paste", (await seen).length);
  console.log("types", clipboardTypes());
  writeFileSync(`${S}/back.png`, clipboardBytes("image/png"));
  // file
  writeFileSync(`${S}/afile.txt`, "x");
  await xcopy(`file://${S}/afile.txt\n`, "text/uri-list");
  await xcopy("filler after file");
  console.log("pasted:", JSON.stringify(await pasteFromHistory(app, ["v"])));
  console.log("types", clipboardTypes(), JSON.stringify(clipboardBytes("text/uri-list").toString()));
  // custom
  await xcopy(new Uint8Array([0, 1, 2, 250, 65]), "application/x-capture-test");
  await xcopy("filler after custom");
  console.log("pasted:", JSON.stringify(await pasteFromHistory(app, ["v"])));
  console.log("types", clipboardTypes(), [...clipboardBytes("application/x-capture-test")]);
} finally { await app.stop(); }
