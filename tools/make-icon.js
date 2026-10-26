const fs = require('fs');
const path = require('path');

function render(size) {
  const ss = 4;
  const w = size * ss;
  const data = new Uint8ClampedArray(w * w * 4);
  const r = 0.235 * w;
  const inset = 0.012 * w;
  const x0 = inset, y0 = inset, x1 = w - inset, y1 = w - inset;
  const cx = (x0 + x1) / 2, cy = (y0 + y1) / 2;
  const hw = (x1 - x0) / 2, hh = (y1 - y0) / 2;

  const c1 = [0x9b, 0x7d, 0xff];
  const c2 = [0x53, 0x3b, 0xd6];

  const poly = [
    [0.265, 0.265], [0.385, 0.265], [0.5, 0.49],
    [0.615, 0.265], [0.735, 0.265], [0.565, 0.765],
    [0.435, 0.765]
  ].map(function (p) { return [p[0] * w, p[1] * w]; });

  function inRounded(x, y, rr) {
    const dx = Math.max(Math.abs(x - cx) - (hw - rr), 0);
    const dy = Math.max(Math.abs(y - cy) - (hh - rr), 0);
    if (Math.abs(x - cx) <= hw - rr && Math.abs(y - cy) <= hh) return true;
    if (Math.abs(y - cy) <= hh - rr && Math.abs(x - cx) <= hw) return true;
    return dx * dx + dy * dy <= rr * rr;
  }

  function inPoly(x, y) {
    let inside = false;
    for (let i = 0, j = poly.length - 1; i < poly.length; j = i++) {
      const xi = poly[i][0], yi = poly[i][1];
      const xj = poly[j][0], yj = poly[j][1];
      const hit = ((yi > y) !== (yj > y)) && (x < (xj - xi) * (y - yi) / (yj - yi) + xi);
      if (hit) inside = !inside;
    }
    return inside;
  }

  for (let y = 0; y < w; y++) {
    for (let x = 0; x < w; x++) {
      const px = x + 0.5, py = y + 0.5;
      if (!inRounded(px, py, r)) continue;
      const t = Math.max(0, Math.min(1,
        ((px - x0) / (x1 - x0)) * 0.45 + ((py - y0) / (y1 - y0)) * 0.55));
      let R = c1[0] + (c2[0] - c1[0]) * t;
      let G = c1[1] + (c2[1] - c1[1]) * t;
      let B = c1[2] + (c2[2] - c1[2]) * t;
      if (!inRounded(px, py, Math.max(1, r - 0.026 * w))) {
        R = R + (255 - R) * 0.42;
        G = G + (255 - G) * 0.42;
        B = B + (255 - B) * 0.42;
      }
      if (inPoly(px, py)) { R = 255; G = 255; B = 255; }
      const i = (y * w + x) * 4;
      data[i] = R;
      data[i + 1] = G;
      data[i + 2] = B;
      data[i + 3] = 255;
    }
  }

  const out = Buffer.alloc(size * size * 4);
  for (let y = 0; y < size; y++) {
    for (let x = 0; x < size; x++) {
      let R = 0, G = 0, B = 0, A = 0;
      for (let sy = 0; sy < ss; sy++) {
        for (let sx = 0; sx < ss; sx++) {
          const i = ((y * ss + sy) * w + (x * ss + sx)) * 4;
          const a = data[i + 3] / 255;
          R += data[i] * a;
          G += data[i + 1] * a;
          B += data[i + 2] * a;
          A += a;
        }
      }
      const o = (y * size + x) * 4;
      if (A > 0) {
        out[o] = Math.round(R / A);
        out[o + 1] = Math.round(G / A);
        out[o + 2] = Math.round(B / A);
        out[o + 3] = Math.round((A / (ss * ss)) * 255);
      }
    }
  }
  return out;
}

function bmpIcon(size) {
  const rgba = render(size);
  const header = Buffer.alloc(40);
  const maskRow = Math.ceil(size / 32) * 4;
  const maskSize = maskRow * size;
  header.writeUInt32LE(40, 0);
  header.writeInt32LE(size, 4);
  header.writeInt32LE(size * 2, 8);
  header.writeUInt16LE(1, 12);
  header.writeUInt16LE(32, 14);
  header.writeUInt32LE(0, 16);
  header.writeUInt32LE(size * size * 4 + maskSize, 20);

  const pixels = Buffer.alloc(size * size * 4);
  for (let y = 0; y < size; y++) {
    for (let x = 0; x < size; x++) {
      const src = ((size - 1 - y) * size + x) * 4;
      const dst = (y * size + x) * 4;
      pixels[dst] = rgba[src + 2];
      pixels[dst + 1] = rgba[src + 1];
      pixels[dst + 2] = rgba[src];
      pixels[dst + 3] = rgba[src + 3];
    }
  }
  const mask = Buffer.alloc(maskSize, 0);
  return Buffer.concat([header, pixels, mask]);
}

const sizes = [16, 24, 32, 48, 64, 128, 256];
const images = sizes.map(function (s) { return { size: s, data: bmpIcon(s) }; });
const header = Buffer.alloc(6);
header.writeUInt16LE(0, 0);
header.writeUInt16LE(1, 2);
header.writeUInt16LE(sizes.length, 4);

const entries = Buffer.alloc(16 * sizes.length);
let offset = 6 + entries.length;
images.forEach(function (img, i) {
  const e = i * 16;
  entries.writeUInt8(img.size >= 256 ? 0 : img.size, e);
  entries.writeUInt8(img.size >= 256 ? 0 : img.size, e + 1);
  entries.writeUInt8(0, e + 2);
  entries.writeUInt8(0, e + 3);
  entries.writeUInt16LE(1, e + 4);
  entries.writeUInt16LE(32, e + 6);
  entries.writeUInt32LE(img.data.length, e + 8);
  entries.writeUInt32LE(offset, e + 12);
  offset += img.data.length;
});

const out = Buffer.concat([header, entries].concat(images.map(function (i) { return i.data; })));
const target = path.join(__dirname, '..', 'assets', 'vexor.ico');
fs.mkdirSync(path.dirname(target), { recursive: true });
fs.writeFileSync(target, out);
console.log('icon written:', target, out.length, 'bytes');
