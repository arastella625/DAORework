"""Renders every piece and turns the renders into the shading masks the app tints.

Run from anywhere (Blender must be closed, or at least not have these files open with unsaved changes):
    blender -b --factory-startup --python export_all.py
Add  -- --no-render  at the end to remake the masks from the renders already in renders/, without rendering.

Each .blend is opened in turn and its own render script runs, so the renders in renders/ are refreshed exactly as if
you had pressed Run Script in each file. Nothing is saved back into the .blend files.

The masks land in export/, one PNG per piece and size, named <piece>@<pixels>.png:
- alpha is the shape (the app tints it with the theme's colour);
- grey is light and shade around 50%: 50% leaves the tint as it is, lighter lightens it, darker darkens it (the app
  lays it over the tint with hard light, like the clickable mock does).
A mark's small sizes come from its -small render (the plain body, no fine detail), its big sizes from the full one.
Each mark also gets <piece>-bare@<pixels>.png: the mark alone, no shield, for looks that draw it flat (Korcari, which
has no shading, so a mark inside a shield would be just a shield).
"""
import os
import sys

import bpy
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
RENDERS, EXPORT = os.path.join(HERE, "renders"), os.path.join(HERE, "export")
PIECES = (("clasp.blend", "render_corners.py"), ("band.blend", "render_band.py"), ("marks.blend", "render_all_marks.py"))

# Which marks go to the app: the picks from ART-PASSES.md, plus the plain shield. Add a mark's file name to export it.
MARKS = ("welcome-open-door", "one-thing-first-signpost", "needs-a-look-nail", "ready-lantern",
         "without-your-mods-candle", "plain")
SMALL, BIG = (24, 34, 46, 48, 68, 92), (96, 128, 192, 256)  # 1x and 2x of where they show
CONTRAST = {"clasp": 1.5, "band": 2.4, "mark": 2.4}  # how strongly light and shade are kept


def render_everything():
    for blend, script in PIECES:
        bpy.ops.wm.open_mainfile(filepath=os.path.join(HERE, blend))
        exec(compile(bpy.data.texts[script].as_string(), f"{blend}:{script}", "exec"), {"__name__": "__main__"})


def load(name):
    img = bpy.data.images.load(os.path.join(RENDERS, f"{name}.png"), check_existing=False)
    w, h = img.size
    a = np.empty(w * h * 4, np.float32)
    img.pixels.foreach_get(a)
    bpy.data.images.remove(img)
    return a.reshape(h, w, 4)[::-1].copy()


def area(n_in, n_out):
    """Area-average resampling: each output pixel is the mean of the input pixels it covers."""
    e = np.linspace(0, n_in, n_out + 1)
    m = np.zeros((n_out, n_in))
    for o in range(n_out):
        for i in range(int(e[o]), int(np.ceil(e[o + 1]))):
            m[o, i] = min(e[o + 1], i + 1) - max(e[o], i)
        m[o] /= m[o].sum()
    return m


def fill_square(rgba, pad=0.06):
    """Crops to the shape (a bare mark is small in the shield's framing) and centres it in a square."""
    ys, xs = np.nonzero(rgba[..., 3] > 0.02)
    cy, cx = (ys.min() + ys.max()) / 2, (xs.min() + xs.max()) / 2
    half = max(ys.max() - ys.min(), xs.max() - xs.min()) / 2 * (1 + 2 * pad)
    out = np.zeros((int(2 * half) + 1,) * 2 + (4,), np.float32)
    for oy in range(out.shape[0]):
        sy = int(round(cy - half + oy))
        if 0 <= sy < rgba.shape[0]:
            x0 = int(round(cx - half))
            lo, hi = max(0, x0), min(rgba.shape[1], x0 + out.shape[1])
            out[oy, lo - x0:hi - x0] = rgba[sy, lo:hi]
    return out


def save_mask(rgba, k, w, h, out_name):
    lum, al = rgba[..., :3].mean(-1), rgba[..., 3]
    shade = np.clip(0.5 + (lum / np.percentile(lum[al > 0.9], 50) - 1) * k * 0.5, 0, 1)
    pre = np.dstack([shade * al, al])  # premultiplied, so edges don't pick up the empty background
    r = np.einsum("pj,ojc->opc", area(rgba.shape[1], w), np.einsum("oi,ijc->ojc", area(rgba.shape[0], h), pre))
    g = r[..., 0] / np.maximum(r[..., 1], 1e-6)
    img = bpy.data.images.new(out_name, w, h, alpha=True)
    img.pixels.foreach_set(np.ascontiguousarray(np.dstack([g, g, g, r[..., 1]])[::-1]).ravel().astype(np.float32))
    img.filepath_raw, img.file_format = os.path.join(EXPORT, f"{out_name}.png"), "PNG"
    img.save()
    bpy.data.images.remove(img)


def export_everything():
    os.makedirs(EXPORT, exist_ok=True)
    written = []
    for corner in ("tl", "tr", "br", "bl"):
        src = load(f"clasp-{corner}")
        for s in (34, 68):
            save_mask(src, CONTRAST["clasp"], s, s, f"clasp-{corner}@{s}")
            written.append(f"clasp-{corner}@{s}")
    src = load("band")
    for h in (80, 160):
        save_mask(src, CONTRAST["band"], h * src.shape[1] // src.shape[0], h, f"band@{h}")
        written.append(f"band@{h}")
    for mark in MARKS:
        name = f"mark-shield-{mark}"
        for level, sizes in ((f"{name}-small", SMALL), (name, BIG)):
            src = load(level)
            for s in sizes:
                save_mask(src, CONTRAST["mark"], s, s, f"{name}@{s}")
                written.append(f"{name}@{s}")
        if mark != "plain":  # the plain shield has no mark to show bare
            src = fill_square(load(f"{name}-bare"))
            for s in SMALL + BIG:
                save_mask(src, CONTRAST["mark"], s, s, f"{name}-bare@{s}")
                written.append(f"{name}-bare@{s}")
    return written


if "--no-render" not in sys.argv:
    render_everything()
done = export_everything()
print(f"EXPORTED {len(done)} masks to {EXPORT}")
