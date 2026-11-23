"""Renders every piece and turns the renders into the shading masks the app tints.

Run from anywhere (Blender must be closed, or at least not have these files open with unsaved changes):
    blender -b --factory-startup --python-exit-code 1 --python export_all.py
Add  -- --no-render  at the end to remake the masks from the renders already in renders/, without rendering.
--python-exit-code 1 makes a failed export end with an error instead of looking like it worked.

Each .blend is opened in turn and its own render script runs, so the renders in renders/ are refreshed exactly as if
you had pressed Run Script in each file. Nothing is saved back into the .blend files.

The masks land in export/, two PNGs per piece and size: <piece>@<size>.png and <piece>@<size>@2x.png, where size is
how big the app shows it, in device-independent pixels (the band's is its height):
- alpha is the shape (the app tints it with the theme's colour);
- grey is light and shade around 50%: 50% leaves the tint as it is, lighter lightens it, darker darkens it (the app
  lays it over the tint with hard light, like the clickable mock does).
A mark shown below 48 uses its -small render (the plain body, no fine detail), from 48 up the full one, at both 1x
and 2x. The Roundel's marks for the Mods tiles come only from its -small render. Each mark also gets
<piece>-bare@<size>.png: the mark alone, no shield, for looks that draw it flat (Korcari, which has no shading, so a
mark inside a shield would be just a shield).
"""
import functools
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
CLASP, BAND = (34,), (80,)  # where they show, in device-independent pixels
SMALL, BIG = (24, 34, 46), (48, 64, 96, 128)
# The Roundel's marks for the Mods tiles (Pass 10). It has only the small drawing, so its sizes stay below 48.
ROUNDEL_MARKS, TILES = ("ready-lantern", "needs-a-look-nail"), (32,)
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


def middle(name):
    """The middle brightness of a render's solid pixels: what 50% grey stands for in every mask shaded against it.
    Each piece is shaded against one render, so the same surface keeps the same tone in every mask and at every size."""
    rgba = load(name)
    solid = rgba[..., :3].mean(-1)[rgba[..., 3] > 0.9]
    if not solid.size:
        raise ValueError(f"renders/{name}.png has nothing solid to measure the shading against")
    return float(np.median(solid))


@functools.lru_cache
def area(n_in, n_out):
    """Area-average resampling: each output pixel is the mean of the input pixels it covers."""
    e = np.linspace(0, n_in, n_out + 1)
    m = np.zeros((n_out, n_in))
    for o in range(n_out):
        for i in range(int(e[o]), int(np.ceil(e[o + 1]))):
            m[o, i] = min(e[o + 1], i + 1) - max(e[o], i)
        m[o] /= m[o].sum()
    return m


def save_mask(rgba, k, ref, w, h, out_name):
    lum, al = rgba[..., :3].mean(-1), rgba[..., 3]
    shade = np.clip(0.5 + (lum / ref - 1) * k * 0.5, 0, 1)
    pre = np.dstack([shade * al, al])  # premultiplied, so edges don't pick up the empty background
    r = np.einsum("pj,ojc->opc", area(rgba.shape[1], w), np.einsum("oi,ijc->ojc", area(rgba.shape[0], h), pre))
    g = r[..., 0] / np.maximum(r[..., 1], 1e-6)
    img = bpy.data.images.new(out_name, w, h, alpha=True)
    img.pixels.foreach_set(np.ascontiguousarray(np.dstack([g, g, g, r[..., 1]])[::-1]).ravel().astype(np.float32))
    img.filepath_raw, img.file_format = os.path.join(EXPORT, f"{out_name}.png"), "PNG"
    img.save()
    bpy.data.images.remove(img)


def save_sizes(rgba, piece, ref, sizes, name):
    """Writes <name>@<size>.png and <name>@<size>@2x.png for every size the piece shows at."""
    written = []
    for size in sizes:
        for h, out_name in ((size, f"{name}@{size}"), (2 * size, f"{name}@{size}@2x")):
            save_mask(rgba, CONTRAST[piece], ref, h * rgba.shape[1] // rgba.shape[0], h, out_name)
            written.append(out_name)
    return written


def export_everything():
    os.makedirs(EXPORT, exist_ok=True)
    clasp, band = middle("clasp-tl"), middle("band")
    big, small = middle("mark-shield-plain"), middle("mark-shield-plain-small")
    written = save_sizes(load("band"), "band", band, BAND, "band")
    for corner in ("tl", "tr", "br", "bl"):
        written += save_sizes(load(f"clasp-{corner}"), "clasp", clasp, CLASP, f"clasp-{corner}")
    for mark in MARKS:
        name = f"mark-shield-{mark}"
        written += save_sizes(load(f"{name}-small"), "mark", small, SMALL, name)
        written += save_sizes(load(name), "mark", big, BIG, name)
        if mark != "plain":  # the plain shield has no mark to show bare
            written += save_sizes(load(f"{name}-bare"), "mark", big, SMALL + BIG, f"{name}-bare")
    tile = middle("mark-roundel-plain-small")
    for mark in ROUNDEL_MARKS:
        name = f"mark-roundel-{mark}"
        written += save_sizes(load(f"{name}-small"), "mark", tile, TILES, name)
        written += save_sizes(load(f"{name}-bare"), "mark", big, TILES, f"{name}-bare")
    return written


if "--no-render" not in sys.argv:
    render_everything()
done = export_everything()
print(f"EXPORTED {len(done)} masks to {EXPORT}")
