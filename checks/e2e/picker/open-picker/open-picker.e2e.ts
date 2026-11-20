// End-to-end check for OpenPicker: where the picker appears, that it leaves the
// keyboard with the app the user is in, and that it drops its preview on a screen
// too short for it. Seen only through the windows on screen and what they show.
import { afterAll, afterEach, beforeAll, expect, test } from "bun:test";
import { TargetApp, unique } from "../../desktop.ts";
import {
  OwnDesktop, cancelThePicker, copyWithXclip, focusedWindowName, holdThePickerOpen, makeImage, movePointer,
  readText, releaseAll, screenSize, shootPicker, squash, waitForPicker,
} from "../picker-driver.ts";

let app: TargetApp;
beforeAll(async () => { app = await TargetApp.start(); }, 90_000);
afterAll(async () => { await releaseAll(); await app?.stop(); });
afterEach(async () => { await releaseAll(); await waitForPicker(false); });

/** Centred, with its top edge 16 % of the way down, or higher when it would otherwise reach below the screen. */
function expectedPlace(box: { width: number; height: number }, screen: { width: number; height: number }) {
  return {
    x: Math.floor(Math.max(0, (screen.width - box.width) / 2)),
    y: Math.floor(Math.max(0, Math.min(screen.height * 0.16, screen.height - box.height))),
  };
}

test("the picker opens centred near the top of the screen", async () => {
  await app.copy(`centred ${unique()}`);
  await app.prepare();
  movePointer(200, 650);
  const box = await holdThePickerOpen();
  const screen = screenSize();
  const place = expectedPlace(box, screen);
  expect(Math.abs(box.x - place.x)).toBeLessThanOrEqual(1);
  expect(Math.abs(box.y - place.y)).toBeLessThanOrEqual(1);
  expect(box.y + box.height).toBeLessThanOrEqual(screen.height);
  await cancelThePicker();
}, 30_000);

test("the picker never takes the keyboard focus", async () => {
  const id = unique();
  await app.copy(`focus ${id}`);
  await app.prepare();
  const before = focusedWindowName();
  await holdThePickerOpen();
  expect(focusedWindowName()).toBe(before);
  await cancelThePicker();
  expect(focusedWindowName()).toBe(before);
}, 30_000);

test("a picker taller than the room below its usual place moves up", async () => {
  await copyWithXclip(makeImage(640, 400), "image/png");
  await app.prepare();
  const box = await holdThePickerOpen();
  const screen = screenSize();
  const place = expectedPlace(box, screen);
  expect(Math.abs(box.y - place.y)).toBeLessThanOrEqual(1);
  expect(box.y + box.height).toBeLessThanOrEqual(screen.height);
  await cancelThePicker();
}, 30_000);

test("the preview is left out when the picker would not fit on the screen", async () => {
  const desktop = await OwnDesktop.start({ width: 1280, height: 480, name: "short-screen" });
  try {
    const d = desktop.display;
    await copyWithXclip("first short line\nsecond short line\nthird short line", undefined, d);
    const box = await holdThePickerOpen(d);
    const text = squash(readText(shootPicker(box, d)));
    await cancelThePicker(d);
    expect(text).toContain("3 lines");
    expect(text).not.toContain("characters");
    expect(box.y + box.height).toBeLessThanOrEqual(480);
  } finally {
    await desktop.stop();
  }
}, 180_000);

test("on a screen with room, the preview shows beside the history", async () => {
  await copyWithXclip(`one ${unique()}\ntwo\nthree`);
  const box = await holdThePickerOpen();
  const text = squash(readText(shootPicker(box)));
  await cancelThePicker();
  expect(text).toContain("3 lines");
  expect(text).toMatch(/3 lines · \d+ characters|3 lines - \d+ characters|3 lines \S? ?\d+ characters/);
}, 30_000);
