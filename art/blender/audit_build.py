"""The build check every pass ends with: what each drawing costs, against a budget for where it shows.

Run from anywhere (it only reads; nothing is saved):
    blender -b --factory-startup --python-exit-code 1 --python audit_build.py
It opens each piece, counts the triangles the render really draws for each drawing (a body at one detail level with
one mark on it), and lists the heaviest parts. It ends with an error if a drawing is over its budget, so
export_all.py can run it first.

Budgets follow where a drawing shows (ADR-0052 §4): detail under a pixel at the largest size it's exported at can't
be seen, so it costs render time and adds nothing.
"""
import os
import sys

import bpy

HERE = os.path.dirname(os.path.abspath(__file__))
# (file, collection holding the drawing, detail level, budget in triangles)
DRAWINGS = (
    ("clasp.blend", "Clasp", None, 4_000),
    ("band.blend", "Band", None, 12_000),
    ("marks.blend", "Shield", "big", 12_000),  # up to 128 px
    ("marks.blend", "Shield", "small", 4_000),  # up to 46 px
    ("marks.blend", "Roundel", "small", 4_000),  # the Mods tiles, 32 px
    ("marks.blend", "Banner", "big", 12_000),  # a crest on its About page, up to 160 px
    ("marks.blend", "Banner", "small", 4_000),  # a crest in a Mods row, 40 px
)
SHAPES = {"MESH", "CURVE", "SURFACE", "FONT", "META"}


def excluded(coll):
    """A collection left out of the view layer (an option not picked) is never evaluated or rendered."""
    def find(lc):
        if lc.collection == coll:
            return lc
        return next((r for c in lc.children for r in [find(c)] if r), None)
    lc = find(bpy.context.view_layer.layer_collection)
    return lc is None or lc.exclude


def drawn(coll, level, mark):
    """The objects a render of this drawing really shows: its body at the level, one mark. A collection's own
    on/off is only the preview (the render script switches each mark on in turn), so it isn't read here."""
    def walk(c):
        if excluded(c):
            return []
        if " body: " in c.name and (not level or not c.name.split(" body: ")[1].startswith(level + " ")):
            return []
        if c.name.startswith(coll.name + ": ") and mark is not None and c is not mark:
            return []
        objs = [o for o in c.objects if o.type in SHAPES and not o.hide_render
                and not (level and level != "big" and o.get("Fine detail"))]
        return objs + [o for k in c.children for o in walk(k)]
    return walk(coll)


def cost(objs):
    dg = bpy.context.evaluated_depsgraph_get()
    parts = []
    for o in objs:
        e = o.evaluated_get(dg)
        m = e.to_mesh()
        parts.append((sum(len(p.vertices) - 2 for p in m.polygons), o.name))
        e.to_mesh_clear()
    return sorted(parts, reverse=True)


over = []
for blend, name, level, budget in DRAWINGS:
    bpy.ops.wm.open_mainfile(filepath=os.path.join(HERE, blend))
    coll = bpy.data.collections[name]
    marks = [k for k in coll.children if " body: " not in k.name and not excluded(k)] or [None]
    worst = max(((sum(t for t, _ in cost(drawn(coll, level, m))), m) for m in marks), key=lambda x: x[0])
    tris, mark = worst
    parts = cost(drawn(coll, level, mark))
    flag = "OVER" if tris > budget else "ok"
    print(f"BUILD {flag:4} {blend:12} {name:8} {level or '-':5} {tris:7d} / {budget:6d} triangles"
          f"  (heaviest mark: {mark.name if mark else '-'})")
    for t, n in parts[:5]:
        print(f"BUILD        {t:7d}  {n}")
    if tris > budget:
        over.append(f"{name} {level or ''}".strip())
if over:
    raise SystemExit(f"over budget: {', '.join(over)}")
print("BUILD every drawing is within its budget")
