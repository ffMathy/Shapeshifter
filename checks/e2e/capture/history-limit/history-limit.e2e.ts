// ail-derived-from: HistoryLimit@0ae42c498412fed2
// End-to-end check for HistoryLimit: how many items the history keeps, and
// pinned items on top. The limit is changed in the settings window, opened
// from the tray icon, as a user would.
import { afterAll, afterEach, beforeAll, expect } from "bun:test";
import { releaseAll, TargetApp, test, unique, waitForPicker } from "../../desktop.ts";
import { closeSettings, pasteFromHistory, pick, pointerAway, setItemsToRemember, waitForNoNotification } from "../capture-driver.ts";

let app: TargetApp;
beforeAll(async () => { app = await TargetApp.start(); pointerAway(); }, 90_000);
afterAll(async () => { await releaseAll(); await closeSettings(); await app?.stop(); });
afterEach(async () => { await releaseAll(); await waitForPicker(false); await waitForNoNotification(); pointerAway(); });

test("the history keeps the newest items, up to Items to remember", async () => {
  await setItemsToRemember(3);
  try {
    const id = unique();
    for (const word of ["first", "second", "third", "fourth"]) await app.copy(`${word} ${id}`);
    // There is no fourth item: 4 does nothing, and the newest is pasted as usual.
    expect(await pasteFromHistory(app, ["4"])).toBe(`fourth ${id}`);
    // The third item is the oldest one kept: "first" was dropped.
    expect(await pasteFromHistory(app, ["3"])).toBe(`second ${id}`);
  } finally {
    await setItemsToRemember(50);
  }
}, 60_000);

test("a pinned item stays on top and keeps its place", async () => {
  const id = unique();
  await app.copy(`filler ${id}`);
  await app.copy(`pinned ${id}`);
  // Pin it: the history's actions are to the right; Pin is the last one.
  await pick(["Right", "Up"], () => app.prepare());
  try {
    await app.copy(`newer ${id}`);
    await app.copy(`newest ${id}`);
    // The choice still starts on the newest copy, so a plain paste is unchanged.
    expect(await pasteFromHistory(app, [])).toBe(`newest ${id}`);
    // The pinned item sits above it.
    expect(await pasteFromHistory(app, ["Up"])).toBe(`pinned ${id}`);
    // Copying the pinned text again leaves it where it is: no second, unpinned copy appears under it.
    await app.copy(`pinned ${id}`);
    expect(await pasteFromHistory(app, ["Up"])).toBe(`pinned ${id}`);
    expect(await pasteFromHistory(app, ["v"])).toBe(`newer ${id}`);
  } finally {
    // Unpin it again, so other checks start without pinned items.
    await pick(["Up", "Right", "Up"], () => app.prepare());
  }
}, 60_000);
