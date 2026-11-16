// node render.js <svg-dir> <out-dir>: rasterizes the Windows look's icons with Chromium (Playwright) and writes
// Application.png, Application.ico (16-256), Tray.png, TrayLight.png and TrayDark.png into <out-dir>.
const { chromium } = require('playwright');
const fs = require('fs');
const path = require('path');
const [src, out] = process.argv.slice(2);
const read = (file) => fs.readFileSync(path.join(src, file), 'utf8');

const small = read('application-small.svg');
const large = read('application.svg');
const tray = read('tray.svg');
const glyph = tray.match(/<g [^>]*>[\s\S]*?<\/g>/)[0];
const trays = {
  'TrayLight.png': tray.replace(glyph, glyph.replace('currentColor', '#1C1C1C')),
  'TrayDark.png': tray.replace(glyph, glyph.replace('currentColor', '#FFFFFF')),
  // One image for both taskbars: a white 1 px outline over a wider, soft dark one.
  'Tray.png': tray.replace(glyph,
    glyph.replace('currentColor', '#1C1C1C" stroke-opacity="0.8').replace('stroke-width="1"', 'stroke-width="2.4"') +
    glyph.replace('currentColor', '#FFFFFF')),
};

// An ICO with 32-bit BMP frames below 256 px (what every Windows icon loader reads) and a PNG frame at 256.
function bmp(size, rgba) {
  const mask = Math.ceil(size / 32) * 4 * size;
  const b = Buffer.alloc(40 + size * size * 4 + mask);
  b.writeUInt32LE(40, 0);
  b.writeInt32LE(size, 4);
  b.writeInt32LE(size * 2, 8);
  b.writeUInt16LE(1, 12);
  b.writeUInt16LE(32, 14);
  b.writeUInt32LE(size * size * 4 + mask, 20);
  for (let y = 0; y < size; y++) {
    for (let x = 0; x < size; x++) {
      const s = ((size - 1 - y) * size + x) * 4;
      const d = 40 + (y * size + x) * 4;
      b[d] = rgba[s + 2]; b[d + 1] = rgba[s + 1]; b[d + 2] = rgba[s]; b[d + 3] = rgba[s + 3];
    }
  }
  return b;
}
function ico(frames) {
  const data = frames.map(([size, png, rgba]) => (size >= 256 ? png : bmp(size, rgba)));
  const header = Buffer.alloc(6 + 16 * frames.length);
  header.writeUInt16LE(1, 2);
  header.writeUInt16LE(frames.length, 4);
  let offset = header.length;
  frames.forEach(([size], i) => {
    const e = 6 + 16 * i;
    header.writeUInt8(size >= 256 ? 0 : size, e);
    header.writeUInt8(size >= 256 ? 0 : size, e + 1);
    header.writeUInt16LE(1, e + 4);
    header.writeUInt16LE(32, e + 6);
    header.writeUInt32LE(data[i].length, e + 8);
    header.writeUInt32LE(offset, e + 12);
    offset += data[i].length;
  });
  return Buffer.concat([header, ...data]);
}

(async () => {
  const browser = await chromium.launch();
  const page = await browser.newPage();
  const render = async (svg, size) => {
    await page.setViewportSize({ width: size, height: size });
    await page.setContent(`<html><body style="margin:0;background:transparent"><img src="data:image/svg+xml;base64,${Buffer.from(svg).toString('base64')}" style="width:${size}px;height:${size}px;display:block"></body></html>`);
    await page.waitForTimeout(30);
    const png = await page.screenshot({ omitBackground: true, clip: { x: 0, y: 0, width: size, height: size } });
    // The same pixels, unpremultiplied, for the BMP frames.
    const rgba = Buffer.from(await page.evaluate(async ([data, size]) => {
      const img = new Image();
      img.src = 'data:image/png;base64,' + data;
      await img.decode();
      const canvas = Object.assign(document.createElement('canvas'), { width: size, height: size });
      const context = canvas.getContext('2d');
      context.drawImage(img, 0, 0);
      return Array.from(context.getImageData(0, 0, size, size).data);
    }, [png.toString('base64'), size]));
    return [png, rgba];
  };
  fs.mkdirSync(out, { recursive: true });
  const frames = [];
  for (const size of [16, 20, 24, 32]) frames.push([size, ...(await render(small, size))]);
  for (const size of [40, 48, 64, 96, 128, 256]) frames.push([size, ...(await render(large, size))]);
  fs.writeFileSync(path.join(out, 'Application.ico'), ico(frames));
  fs.writeFileSync(path.join(out, 'Application.png'), frames[frames.length - 1][1]);
  // The tray images are 32 px with a 2 px stroke on whole pixels, so halving them for a 16 px tray stays crisp.
  for (const [name, svg] of Object.entries(trays)) fs.writeFileSync(path.join(out, name), (await render(svg, 32))[0]);
  await browser.close();
})();
