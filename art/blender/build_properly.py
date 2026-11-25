"""The "Built properly" pass: each piece drawn with what its largest size can show, and no more.

Run it inside a piece's .blend (Scripting tab, Run Script) or from a terminal:
    blender -b marks.blend --factory-startup --python-exit-code 1 --python build_properly.py
It works on the open file and saves it. Everything it changes is non-destructive and named, so it can be run again
(it does nothing twice) and undone by hand:
- modifiers it adds are named "Build: ...";
- modifiers it switches off and objects it hides keep their settings, with the property "Build: why" on the object;
- collections it leaves out of the view layer are untouched otherwise: tick them back in the Outliner to use them.
audit_build.py measures the result against the budgets.
"""
import bpy
import bmesh

O, C = bpy.data.objects, bpy.data.collections
FILE = bpy.path.basename(bpy.data.filepath)
WHY = "Build: why"

# Options that weren't picked: kept in the file, left out of every render and of the scene's evaluation.
UNUSED = {
    "marks.blend": ("Shield: ready (tick)", "Shield: needs a look (!)", "Shield: welcome (archway)",
                    "Shield: one thing first (arrow)", "Shield: ready (chiselled tick)",
                    "Shield: without your mods (empty hook)", "Roundel: ready (tick)", "Roundel: needs a look (!)",
                    "Roundel body: other (oak targe)", "Roundel body: old (blockout)", "Banner: ready (tick)",
                    "Banner: needs a look (!)", "Banner: add-in (scroll)", "Banner: override (patch)",
                    "Banner: runs in the game (brazier)", "Banner body: old (blockout)"),
    "clasp.blend": ("Clasp: old rivets (off)",),
}
TURNS = {"Roundel boss (iron)": 40, "Crossbar": 24, "Crossbar, plain": 24, "Sleeve": 24, "Sleeve, plain": 24,
         "Finial left": 24, "Finial right": 24, "Tassel left": 12, "Tassel right": 12}  # Screw steps by size
CLOTH = {"Banner cloth (folds are a shape key)": 1}  # un-subdivide iterations (a regular grid with folds)
FLAT_CLOTH = {"Banner cloth, plain": 0.15}  # the small drawing's flat cloth: collapse ratio (its notch breaks the grid)
DECIMATE = {"Stud between motifs": 0.2, "Stitches": 0.4}  # the band's repeated small parts: collapse ratio
# Sub-pixel strokes kept on purpose: many of them together read as a texture (a darned grid, a frayed hem, a crack).
KEEP_THIN = ("Darning", "Frayed threads", "Cracked pane", "Cracked pane branch")


def note(o, why):
    """Say why on the object; a second reason is added, never written over the first."""
    if why not in o.get(WHY, ""):
        o[WHY] = (o[WHY] + "; " + why) if o.get(WHY) else why


def leave_out(names):
    def find(lc, coll):
        if lc.collection == coll:
            return lc
        return next((r for c in lc.children for r in [find(c, coll)] if r), None)
    for name in names:
        if name in C:
            lc = find(bpy.context.view_layer.layer_collection, C[name])
            if lc:
                lc.exclude = True


def first(o, mod):
    """Put a Build: modifier at the top of the stack, so it runs before the motion and the thickness."""
    o.modifiers.move(o.modifiers.find(mod.name), 0)


def lighter_cloth():
    for name, its in CLOTH.items():
        o = O.get(name)
        if o and not o.modifiers.get("Build: lighter cloth (un-subdivide)"):
            d = o.modifiers.new("Build: lighter cloth (un-subdivide)", "DECIMATE")
            d.decimate_type, d.iterations = "UNSUBDIV", its
            first(o, d)
            note(o, f"cloth grid was finer than its largest size shows: un-subdivided {its} time(s), folds kept;"
                    " its Thickness keeps only the top and the rim (the back faces away from every camera)")
        for m in o.modifiers if o else ():
            if m.type == "SOLIDIFY":
                m.use_rim_only = True


def lighter_flat_cloth():
    for name, ratio in FLAT_CLOTH.items():
        o = O.get(name)
        if o and o.modifiers.get("Build: lighter cloth (decimate)"):
            o.modifiers["Build: lighter cloth (decimate)"].ratio = ratio  # a re-run takes a changed ratio
        elif o:
            for m in [m for m in o.modifiers if m.name == "Build: lighter cloth (un-subdivide)"]:
                o.modifiers.remove(m)  # an earlier try: un-subdividing stops at the notch
            d = o.modifiers.new("Build: lighter cloth (decimate)", "DECIMATE")
            d.decimate_type, d.ratio = "COLLAPSE", ratio
            first(o, d)
            note(o, f"flat cloth for 40 px: {int(ratio * 100)}% of its faces still bends with the wind;"
                    " its Thickness keeps only the top and the rim")
        for m in o.modifiers if o else ():
            if m.type == "SOLIDIFY":
                m.use_rim_only = True


def rounder_than_needed():
    """Circles drawn with 48 or more points (the wheel, its hub, the key's bow, and their cords): every other
    point goes. 32 points keeps a circle round at 160 px; the shape and where it sits don't change."""
    for o in O:
        if o.type != "CURVE" or o.library or o.get("Build: circle points halved"):
            continue
        cu, changed = o.data, False
        for sp in [sp for sp in cu.splines if sp.type == "POLY" and sp.use_cyclic_u and len(sp.points) >= 48]:
            keep = [tuple(p.co) for i, p in enumerate(sp.points) if i % 2 == 0]
            new = cu.splines.new("POLY")  # a spline's points can't be removed, so it's drawn again with half
            new.points.add(len(keep) - 1)
            for p, co in zip(new.points, keep):
                p.co = co
            new.use_cyclic_u, new.material_index, new.use_smooth = True, sp.material_index, sp.use_smooth
            cu.splines.remove(sp)
            changed = True
        if changed:
            o["Build: circle points halved"] = True


def lighter_band():
    for name, ratio in DECIMATE.items():
        o = O.get(name)
        if o and not o.modifiers.get("Build: lighter (decimate)"):
            d = o.modifiers.new("Build: lighter (decimate)", "DECIMATE")
            d.decimate_type, d.ratio = "COLLAPSE", ratio
            note(o, f"repeated small part: {int(ratio * 100)}% of its triangles is all 160 px can show")


def turns():
    for name, steps in TURNS.items():
        o = O.get(name)
        s = next((m for m in o.modifiers if m.type == "SCREW"), None) if o else None
        if s and s.steps > steps:
            s.steps = s.render_steps = steps
            note(o, f"turned {steps} times: enough for where it shows")


def roundel_boss():
    o = O.get("Roundel boss (iron)")
    if not o or o.get("Build: profile halved"):  # its own marker: halving twice would lose the shape
        return
    for m in o.modifiers:  # the knock can't be seen at 32 px; its Thickness was only there for the cut
        if m.type in ("BOOLEAN", "SOLIDIFY"):
            m.show_viewport = m.show_render = False
    bm = bmesh.new()
    bm.from_mesh(o.data)
    bm.verts.ensure_lookup_table()
    ends = {v for v in bm.verts if len(v.link_edges) < 2}
    order = sorted(bm.verts, key=lambda v: (v.co.x, -v.co.z))
    drop = [v for i, v in enumerate(order) if i % 2 == 1 and v not in ends]
    bmesh.ops.dissolve_verts(bm, verts=drop)
    bm.to_mesh(o.data)
    bm.free()
    o["Build: profile halved"] = True
    note(o, "profile halved, knock and thickness off: 32 px can't show them")


def curves():
    for o in O:
        if o.type != "CURVE" or o.library:
            continue
        cu = o.data
        if cu.resolution_u > 6:
            cu.resolution_u = 6
        if cu.bevel_depth < 0.008 and cu.bevel_resolution > 1:
            cu.bevel_resolution = 1
        thin = cu.bevel_depth * 2 + cu.extrude * 2 < 0.006 and cu.extrude == 0 and cu.bevel_depth > 0
        if thin and o.name.endswith("couching") and not o.hide_render:
            o.hide_render = o.hide_viewport = True
            note(o, "under a pixel at the largest size: couching stitches can't be seen, so they aren't drawn")
        elif thin and o.name.split(".")[0] in KEEP_THIN:
            note(o, "under a pixel, kept: together its strokes read as a texture")


def render_script_skips_left_out():
    """render_all_marks.py renders every mark collection; one left out of the view layer is skipped, not rendered
    as an empty body."""
    t = bpy.data.texts.get("render_all_marks.py")
    if not t or "in_scene(" in t.as_string():
        return
    s = t.as_string()
    nl = chr(10)
    loop = "xs = {n: O[n].location.x for n in starters}" + nl
    helper = nl.join([
        "def in_scene(lc):",
        '    """Collections left out of the view layer (options not picked) are not rendered."""',
        "    return [lc.collection.name] + [n for c in lc.children if not c.exclude for n in in_scene(c)]",
        "",
        "",
        "in_view = set(in_scene(bpy.context.view_layer.layer_collection))",
        ""])
    marks = '    marks = [k for k in kids if " body: " not in k.name]' + nl
    assert s.count(loop) == 1 and s.count(marks) == 1, "render_all_marks.py changed: update build_properly.py"
    s = s.replace(loop, helper + loop).replace(
        marks, '    marks = [k for k in kids if " body: " not in k.name and k.name in in_view]' + nl)
    t.from_string(s)


def render_settings():
    for sc in bpy.data.scenes:
        cy = sc.cycles
        cy.device = "GPU"  # used when export_all.py finds a GPU; Cycles falls back to the CPU when there's none
        cy.max_bounces, cy.diffuse_bounces, cy.glossy_bounces, cy.transmission_bounces = 4, 2, 2, 2
        sc.render.use_persistent_data = True  # keep the scene between frames, so a clip isn't rebuilt each frame


leave_out(UNUSED.get(FILE, ()))
if FILE == "marks.blend":
    render_script_skips_left_out()
    roundel_boss()
    lighter_cloth()
    lighter_flat_cloth()
    rounder_than_needed()
    turns()
    curves()
if FILE == "band.blend":
    lighter_band()
render_settings()
bpy.ops.wm.save_mainfile()
print("BUILT PROPERLY", FILE)
