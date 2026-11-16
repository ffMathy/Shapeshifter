// node render.js <svg-dir> <out-dir>: rasterizes the Linux look's icons with Chromium.
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');
const [src, out] = process.argv.slice(2);
const jobs = [['application.svg', 256, 'Application.png'], ['tray.svg', 32, 'Tray.png']];
(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  fs.mkdirSync(out, { recursive: true });
  for (const [file, size, name] of jobs) {
    const svg = fs.readFileSync(path.join(src, file));
    await page.setViewportSize({ width: size, height: size });
    await page.setContent(`<html><body style="margin:0;background:transparent"><img src="data:image/svg+xml;base64,${svg.toString('base64')}" style="width:${size}px;height:${size}px;display:block"></body></html>`);
    await page.waitForTimeout(30);
    await page.screenshot({ path: path.join(out, name), omitBackground: true, clip: { x: 0, y: 0, width: size, height: size } });
  }
  await browser.close();
})();
