# Objetos e ambientes do Tripo e do Meshy que entram no jogo (fora da vitrine da Aldeia, que tem o
# objetos_vitrine.py próprio): armas, itens, rádio, gravador, tabuleta, helicóptero e o Selo de Kur.
#
# Lê o FBX bruto (ArteFonte/<Objetos|Ambientes>/<Nome>/1_Meshy, fora do Git), solda as costuras, gira para a frente
# ficar no +z da Unity, põe na escala real, reduz os triângulos até o orçamento e exporta um FBX por objeto em
# Assets/_Nibiru/Art/<Objetos|Ambientes>/<Nome>/, com as texturas reduzidas ao lado (<Nome>_cor.jpg; os do Meshy
# também <Nome>_normal.png e <Nome>_mascara.png, a máscara do URP: R metal, G oclusão, A lisura).
# O menu "Nibiru > Arte > Objetos e ambientes" da Unity cria os materiais, os prefabs e troca os placeholders.
#
# Rodar (o caminho do Blender pode mudar), da pasta do projeto:
#   blender -b --factory-startup -P ArteFonte/Objetos/objetos_jogo.py -- <pasta do projeto> [nomes...]
#
# Pivô: armas na ponta do cano (ou da lâmina), no eixo do cano; o resto no centro do pé. O helicóptero tem o pivô
# no chão embaixo do eixo do rotor e sai em duas peças (Corpo e RotorPrincipal, com a origem no eixo de giro), para
# o Spinner do rotor do blockout girar as pás.
#
# Eixos: o FBX chega na Unity como (x, z, y) do Blender; o +z da Unity é o +y do Blender. Os brutos vêm todos com
# a frente para -y do Blender (o cano, o bico, a face da tabuleta, o painel do rádio, o nariz do helicóptero).

import math
import os
import sys

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Vector

# nome: (pasta da ArteFonte, arquivo bruto, giro, (eixo, medida em m), orçamento de triângulos, textura, pivô)
#   giro: "frente" gira 180 graus em z (frente para +z da Unity); "deitar" deita a face para cima.
#   eixo da medida: x largura, y comprimento (frente-trás), z altura (do Blender, depois do giro).
#   pivô: "cano" (ponta em +y, no eixo), "pe" (centro do pé) ou "rotor" (helicóptero).
OBJETOS = {
    "Fuzil": ("Objetos/Fuzil", "Fuzil_Soldados.fbx", "frente", ("y", 0.94), 20000, 2048, "cano"),
    "Pistola": ("Objetos/Pistola", "Pistola_Makarov.fbx", "frente", ("y", 0.161), 20000, 2048, "cano"),
    "Faca": ("Objetos/Faca", "Faca.fbx", "frente", ("y", 0.28), 20000, 2048, "cano"),
    "Espingarda": ("Objetos/Espingarda", "Espingarda.fbx", "frente", ("y", 1.15), 20000, 2048, "cano"),
    "TabuletaDeJunco": ("Objetos/TabuletaDeJunco", "Tabuletadejunco.fbx", "frente", ("x", 0.18), 10000, 2048, "pe"),
    "Radio": ("Objetos/Radio", "Radio.fbx", "frente", ("x", 0.40), 10000, 1024, "pe"),
    "Antibioticos": ("Objetos/Antibioticos", "*.fbx", "frente", ("z", 0.12), 10000, 1024, "pe"),
    "CaixaMunicao": ("Objetos/CaixaMunicao", "*.fbx", "frente", ("x", 0.12), 10000, 1024, "pe"),
    "Curativo": ("Objetos/Curativo", "*.fbx", "deitar", ("x", 0.16), 10000, 1024, "pe"),
    "RoloFilme": ("Objetos/RoloFilme", "*.fbx", "frente", ("z", 0.06), 10000, 1024, "pe"),
    "Gravador": ("Objetos/Gravador", "*.fbx", "frente", ("x", 0.24), 10000, 1024, "pe"),
    "Helicoptero": ("Ambientes/Helicoptero", "helicopter_3d_model.fbx", "frente", ("y", 10.0), 50000, 2048, "rotor"),
    "SeloDeKur": ("Ambientes/SeloDeKur", "SeloDeKur.fbx", "frente", ("x", 6.0), 48000, 2048, "pe"),
}

AXIS = {"x": 0, "y": 1, "z": 2}


# ---------- utilidades ----------

def find_raw(root, folder, pattern):
    base = os.path.join(root, "ArteFonte", folder, "1_Meshy")
    if pattern != "*.fbx":
        return os.path.join(base, pattern)
    for dirpath, _, files in os.walk(base):
        for f in files:
            if f.lower().endswith(".fbx"):
                return os.path.join(dirpath, f)
    raise FileNotFoundError(base)


def load(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=path)
    ob = next(o for o in bpy.data.objects if o.type == 'MESH')
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return ob


def bounds(bm, verts=None):
    vs = verts if verts is not None else bm.verts
    xs = [v.co.x for v in vs]; ys = [v.co.y for v in vs]; zs = [v.co.z for v in vs]
    return Vector((min(xs), min(ys), min(zs))), Vector((max(xs), max(ys), max(zs)))


def tris(ob):
    ob.data.calc_loop_triangles()
    return len(ob.data.loop_triangles)


def decimate(ob, budget):
    n = tris(ob)
    if n <= budget:
        return n
    mod = ob.modifiers.new("Decimate", 'DECIMATE')
    mod.decimate_type = 'COLLAPSE'
    mod.ratio = budget / n * 0.98
    mod.use_collapse_triangulate = True
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.modifier_apply(modifier=mod.name)
    return tris(ob)


def smooth(ob):
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.select_all(action='DESELECT')
    ob.select_set(True)
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(35))


def islands(bm):
    seen, out = set(), []
    for v in bm.verts:
        if v in seen:
            continue
        stack, grp = [v], []
        seen.add(v)
        while stack:
            x = stack.pop()
            grp.append(x)
            for e in x.link_edges:
                o = e.other_vert(x)
                if o not in seen:
                    seen.add(o)
                    stack.append(o)
        out.append(grp)
    return out


# ---------- texturas ----------

def images_of(ob):
    nodes = ob.active_material.node_tree.nodes if ob.active_material and ob.active_material.node_tree else []
    return [n.image for n in nodes if n.type == 'TEX_IMAGE' and n.image]


def loose_textures(raw):
    """Os do Meshy trazem as PNG soltas ao lado do FBX: cor, normal, metal e aspereza."""
    folder = os.path.dirname(raw)
    found = {}
    for f in os.listdir(folder):
        low = f.lower()
        if not low.endswith(".png"):
            continue
        if low.endswith("_normal.png"):
            found["normal"] = f
        elif low.endswith("_metallic_roughness.png"):
            continue
        elif low.endswith("_metallic.png"):
            found["metal"] = f
        elif low.endswith("_roughness.png"):
            found["rough"] = f
        elif low.endswith("_texture.png"):
            found["cor"] = f
    return {k: os.path.join(folder, v) for k, v in found.items()}


def load_image(path, size, non_color=False):
    img = bpy.data.images.load(path, check_existing=False)
    if non_color:
        img.colorspace_settings.name = 'Non-Color'
    if img.size[0] != size:
        img.scale(size, size)
    return img


def save_jpg(img, path):
    img.filepath_raw = path
    img.file_format = 'JPEG'
    img.save(filepath=path, quality=90)


def save_png(img, path):
    img.filepath_raw = path
    img.file_format = 'PNG'
    img.save(filepath=path)


def write_textures(ob, raw, out, name, size):
    loose = loose_textures(raw)
    if "cor" in loose:
        save_jpg(load_image(loose["cor"], size), os.path.join(out, f"{name}_cor.jpg"))
    else:
        img = images_of(ob)[0]
        if img.size[0] != size:
            img.scale(size, size)
        save_jpg(img, os.path.join(out, f"{name}_cor.jpg"))
    written = ["cor"]
    if "normal" in loose:
        save_png(load_image(loose["normal"], size, True), os.path.join(out, f"{name}_normal.png"))
        written.append("normal")
    if "metal" in loose or "rough" in loose:
        m = min(size, 1024)
        px = np.ones((m * m, 4), dtype=np.float32)
        if "metal" in loose:
            a = np.empty(m * m * 4, dtype=np.float32)
            load_image(loose["metal"], m, True).pixels.foreach_get(a)
            px[:, 0] = a.reshape(-1, 4)[:, 0]
        else:
            px[:, 0] = 0.0
        if "rough" in loose:
            a = np.empty(m * m * 4, dtype=np.float32)
            load_image(loose["rough"], m, True).pixels.foreach_get(a)
            px[:, 3] = 1.0 - a.reshape(-1, 4)[:, 0]
        else:
            px[:, 3] = 0.3
        px[:, 2] = 0.0
        mask = bpy.data.images.new(f"{name}_mascara", m, m, alpha=True)
        mask.colorspace_settings.name = 'Non-Color'
        mask.pixels.foreach_set(px.ravel())
        save_png(mask, os.path.join(out, f"{name}_mascara.png"))
        written.append("mascara")
    return written


# ---------- exportação ----------

def export(path, obs):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in obs:
        ob.select_set(True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_scale_options='FBX_SCALE_UNITS',
                             axis_forward='-Z', axis_up='Y', bake_space_transform=True, mesh_smooth_type='OFF',
                             add_leaf_bones=False, bake_anim=False, object_types={'MESH'}, path_mode='STRIP')


def piece(name, bm, faces, origin, material):
    """Tira as faces do bmesh para uma peça nova, com a origem do objeto em `origin`."""
    b2 = bmesh.new()
    uv_src = bm.loops.layers.uv.active
    uv = b2.loops.layers.uv.new()
    vmap = {}
    for f in faces:
        vs = []
        for v in f.verts:
            if v not in vmap:
                vmap[v] = b2.verts.new(v.co - origin)
            vs.append(vmap[v])
        nf = b2.faces.new(vs)
        for l_new, l_old in zip(nf.loops, f.loops):
            l_new[uv].uv = l_old[uv_src].uv
    me = bpy.data.meshes.new(name)
    b2.to_mesh(me)
    b2.free()
    me.materials.append(material)
    ob = bpy.data.objects.new(name, me)
    ob.location = origin
    bpy.context.collection.objects.link(ob)
    return ob


# ---------- processamento ----------

def process(root, name, spec):
    folder, pattern, turn, (axis, size), budget, tex, pivot = spec
    raw = find_raw(root, folder, pattern)
    kind = folder.split("/")[0]
    out = os.path.join(root, "Assets", "_Nibiru", "Art", kind, name)
    os.makedirs(out, exist_ok=True)

    ob = load(raw)
    raw_tris = tris(ob)
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    welded = len(bm.verts)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    welded -= len(bm.verts)

    if turn == "frente":
        bm.transform(Matrix.Rotation(math.pi, 4, 'Z'))
    elif turn == "deitar":
        bm.transform(Matrix.Rotation(-math.pi / 2, 4, 'X'))

    mn, mx = bounds(bm)
    s = size / (mx - mn)[AXIS[axis]]
    bm.transform(Matrix.Scale(s, 4))
    mn, mx = bounds(bm)

    if pivot == "cano":
        # Ponta do cano (ou da lâmina) em +y; o eixo é a média dos vértices dos 3% da frente.
        tip = [v.co for v in bm.verts if v.co.y > mx.y - 0.03 * (mx.y - mn.y)]
        axis_pt = sum(tip, Vector()) / len(tip)
        origin = Vector((axis_pt.x, mx.y, axis_pt.z))
    else:
        origin = Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, mn.z))

    rotor_parts = None
    if pivot == "rotor":
        rotor_parts = split_rotors(bm, mn, mx)
        hub = rotor_parts["hub"]
        origin = Vector((hub.x, hub.y, mn.z))

    bm.transform(Matrix.Translation(-origin))
    bm.to_mesh(ob.data)
    bm.free()
    ob.data.update()
    material = ob.active_material

    textures = write_textures(ob, raw, out, name, tex)

    obs = [ob]
    if rotor_parts:
        bm = bmesh.new()
        bm.from_mesh(ob.data)
        idx = set(rotor_parts["main"])
        faces = [f for f in bm.faces if all(v.index in idx for v in f.verts)]
        hub_local = rotor_parts["hub"] - origin
        obs.append(piece(f"{name}_RotorPrincipal", bm, faces, hub_local, material))
        bmesh.ops.delete(bm, geom=faces, context='FACES')
        bm.to_mesh(ob.data)
        bm.free()
        ob.data.update()
        print(f"[{name}] rotor no eixo, a {hub_local.z:.3f} m do chão; {len(faces)} faces")

    ob.name = name if not rotor_parts else f"{name}_Corpo"
    ob.data.name = ob.name
    total = 0
    for o in obs:
        decimate(o, int(budget * tris(o) / max(1, sum(tris(p) for p in obs))))
        smooth(o)
        total += tris(o)
    mn, mx = Vector((1e9,) * 3), Vector((-1e9,) * 3)
    for o in obs:
        for v in o.data.vertices:
            p = o.matrix_world @ v.co
            mn = Vector(map(min, mn, p)); mx = Vector(map(max, mx, p))
    print(f"[{name}] bruto {raw_tris} tris, soldados {welded}; final {total} tris; "
          f"tamanho (x, altura, z da Unity) {round(mx.x - mn.x, 3)} x {round(mx.z - mn.z, 3)} x {round(mx.y - mn.y, 3)} m; "
          f"texturas {textures} {tex} px")
    export(os.path.join(out, f"{name}.fbx"), obs)


def split_rotors(bm, mn, mx):
    """Pás e cubo do rotor principal: as peças soltas acima do teto da cabine, fora da cauda (o nariz já está em +y).
    O rotor de cauda fica preso ao corpo: o modelo tem peças dos dois lados da cauda e não dá para separar com
    segurança só pela forma."""
    bm.verts.ensure_lookup_table()
    h = mx.z - mn.z
    length = mx.y - mn.y
    main = []
    for g in islands(bm):
        a, b = bounds(bm, g)
        if len(g) < 0.2 * len(bm.verts) and a.z > mn.z + 0.72 * h and a.y > mn.y + 0.25 * length:
            main += [v.index for v in g]
    pts = [bm.verts[i].co for i in main]
    a = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    b = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    print(f"[rotor] {len(main)} vértices no principal (raio {round(max(b.x - a.x, b.y - a.y) / 2, 2)} m)")
    return {"main": main, "hub": (a + b) / 2}


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    root, only = args[0], args[1:]
    for name, spec in OBJETOS.items():
        if not only or name in only:
            process(root, name, spec)


main()
