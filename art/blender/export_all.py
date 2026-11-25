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

Pass 23's clips (ADR-0054): marks.blend's render_clips.py renders each -arrive clip into renders/clips/, and each
becomes one strip of masks, frame after frame left to right, <piece>@<size>-arrive.png and <piece>@<size>@2x-arrive.png.
The export stops if a clip has more than 48 frames, if its last frame isn't exactly its still, or if a 2x strip is
over 400 KB.
"""
import functools
import os
import sys

import bpy
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
RENDERS, EXPORT = os.path.join(HERE, "renders"), os.path.join(HERE, "export")
PIECES = (("clasp.blend", "render_corners.py"), ("band.blend", "render_band.py"), ("marks.blend", "render_all_marks.py"),
          ("marks.blend", "render_clips.py"))

CLASP, BAND = (34,), (80,)  # where they show, in device-independent pixels
# Which marks go to the app, per body: the picks from ART-PASSES.md, the sizes drawn from its -small render (below 48)
# and from its full one (48 and up). Add a mark's file name to export it. Each plain body is exported too; the
# Roundel (the Mods tiles, Pass 10) has only the small drawing.
MARKS = {
    "shield": (("welcome-open-door", "one-thing-first-signpost", "needs-a-look-nail", "ready-lantern",
                "without-your-mods-candle", "plain"), (24, 34, 46), (48, 64, 96, 128)),
    "roundel": (("ready-lantern", "needs-a-look-nail"), (32,), ()),
    # The crest of a mod with no picture, by kind (Pass 13 and 14): a Mods row (40), a card and its About page.
    "banner": (("add-in-key", "override-hammer", "runs-in-the-game-wheel", "plain"), (40,), (96, 160)),
}
# Pass 23's -arrive clips: the clip's render name -> (the piece it animates, the still it must end on, the plain render
# it's shaded against, where it shows). A headline mark at 64, a Mods tile at 32, a crest in a Mods row at 40.
CLIPS = {
    "welcome": ("mark-shield-welcome-open-door", "mark-shield-welcome-open-door", "mark-shield-plain", (64,)),
    "ready": ("mark-shield-ready-lantern", "mark-shield-ready-lantern", "mark-shield-plain", (64,)),
    "needs-a-look": ("mark-shield-needs-a-look-nail", "mark-shield-needs-a-look-nail", "mark-shield-plain", (64,)),
    "one-thing-first": ("mark-shield-one-thing-first-signpost", "mark-shield-one-thing-first-signpost",
                        "mark-shield-plain", (64,)),
    "without-your-mods": ("mark-shield-without-your-mods-candle", "mark-shield-without-your-mods-candle",
                          "mark-shield-plain", (64,)),
    "tile-ready": ("mark-roundel-ready-lantern", "mark-roundel-ready-lantern-small", "mark-roundel-plain-small", (32,)),
    "tile-needs-a-look": ("mark-roundel-needs-a-look-nail", "mark-roundel-needs-a-look-nail-small",
                          "mark-roundel-plain-small", (32,)),
    "crest-add-in-key": ("mark-banner-add-in-key", "mark-banner-add-in-key-small", "mark-banner-plain-small", (40,)),
    "crest-override-hammer": ("mark-banner-override-hammer", "mark-banner-override-hammer-small",
                              "mark-banner-plain-small", (40,)),
    "crest-runs-in-the-game-wheel": ("mark-banner-runs-in-the-game-wheel", "mark-banner-runs-in-the-game-wheel-small",
                                     "mark-banner-plain-small", (40,)),
}
CLIP_FRAMES, CLIP_BYTES = 48, 400_000  # ADR-0054's budget: frames in a clip, bytes in a 2x strip
CONTRAST = {"clasp": 1.5, "band": 2.4, "mark": 2.4}  # how strongly light and shade are kept
LINKED = ("objects", "collections", "worlds", "materials", "node_groups", "meshes")  # what pieces link (ADR-0053)


def render_everything():
    for blend, script in PIECES:
        bpy.ops.wm.open_mainfile(filepath=os.path.join(HERE, blend))
        missing = [f"{block.name} ({kind})" for kind in LINKED for block in getattr(bpy.data, kind) if block.is_missing]
        if missing:  # a renamed or missing rig.blend or materials.blend would render pink or unlit into the masks
            raise RuntimeError(f"{blend} can't find what it links: {', '.join(missing)}")
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


def make_mask(rgba, k, ref, w, h):
    lum, al = rgba[..., :3].mean(-1), rgba[..., 3]
    shade = np.clip(0.5 + (lum / ref - 1) * k * 0.5, 0, 1)
    pre = np.dstack([shade * al, al])  # premultiplied, so edges don't pick up the empty background
    r = np.einsum("pj,ojc->opc", area(rgba.shape[1], w), np.einsum("oi,ijc->ojc", area(rgba.shape[0], h), pre))
    g = r[..., 0] / np.maximum(r[..., 1], 1e-6)
    return np.dstack([g, g, g, r[..., 1]])


def write_png(mask, out_name):
    h, w = mask.shape[:2]
    path = os.path.join(EXPORT, f"{out_name}.png")
    img = bpy.data.images.new(out_name, w, h, alpha=True)
    img.pixels.foreach_set(np.ascontiguousarray(mask[::-1]).ravel().astype(np.float32))
    img.filepath_raw, img.file_format = path, "PNG"
    img.save()
    bpy.data.images.remove(img)
    return os.path.getsize(path)


def save_mask(rgba, k, ref, w, h, out_name):
    write_png(make_mask(rgba, k, ref, w, h), out_name)


def save_sizes(rgba, piece, ref, sizes, name):
    """Writes <name>@<size>.png and <name>@<size>@2x.png for every size the piece shows at."""
    written = []
    for size in sizes:
        for h, out_name in ((size, f"{name}@{size}"), (2 * size, f"{name}@{size}@2x")):
            save_mask(rgba, CONTRAST[piece], ref, h * rgba.shape[1] // rgba.shape[0], h, out_name)
            written.append(out_name)
    return written


def export_clips():
    """Each clip's frames become one strip per size, after three checks (ADR-0054 §5 and §6)."""
    written = []
    for clip, (piece, still, plain, sizes) in CLIPS.items():
        frames = sorted(f[:-4] for f in os.listdir(os.path.join(RENDERS, "clips"))
                        if f.startswith(clip + "-") and f[len(clip) + 1:-4].isdigit())
        if not frames or len(frames) > CLIP_FRAMES:
            raise ValueError(f"clip {clip} has {len(frames)} frames; a clip has 1 to {CLIP_FRAMES}")
        renders = [load(os.path.join("clips", f)) for f in frames]
        if (np.abs(renders[-1] - load(still)).max(-1) > 0.1).any():  # it must rest on exactly the still
            raise ValueError(f"clip {clip} doesn't end on its still, renders/{still}.png")
        ref = middle(plain)
        for size in sizes:
            for h, out_name in ((size, f"{piece}@{size}-arrive"), (2 * size, f"{piece}@{size}@2x-arrive")):
                strip = np.concatenate([make_mask(r, CONTRAST["mark"], ref, h, h) for r in renders], 1)
                if write_png(strip, out_name) > CLIP_BYTES and h == 2 * size:
                    raise ValueError(f"{out_name}.png is over ADR-0054's {CLIP_BYTES // 1000} KB")
                written.append(out_name)
    return written


def export_everything():
    os.makedirs(EXPORT, exist_ok=True)
    clasp, band = middle("clasp-tl"), middle("band")
    bare = middle("mark-shield-plain")  # a bare mark is the same mark on any body, so one reference for all
    written = save_sizes(load("band"), "band", band, BAND, "band")
    for corner in ("tl", "tr", "br", "bl"):
        written += save_sizes(load(f"clasp-{corner}"), "clasp", clasp, CLASP, f"clasp-{corner}")
    for body, (marks, small_sizes, big_sizes) in MARKS.items():
        small = middle(f"mark-{body}-plain-small")
        big = middle(f"mark-{body}-plain") if big_sizes else None
        for mark in marks:
            name = f"mark-{body}-{mark}"
            written += save_sizes(load(f"{name}-small"), "mark", small, small_sizes, name)
            if big_sizes:
                written += save_sizes(load(name), "mark", big, big_sizes, name)
            if mark != "plain":  # a plain body has no mark to show bare
                written += save_sizes(load(f"{name}-bare"), "mark", bare, small_sizes + big_sizes, f"{name}-bare")
    return written + export_clips()


if "--no-render" not in sys.argv:
    render_everything()
done = export_everything()
print(f"EXPORTED {len(done)} masks to {EXPORT}")
