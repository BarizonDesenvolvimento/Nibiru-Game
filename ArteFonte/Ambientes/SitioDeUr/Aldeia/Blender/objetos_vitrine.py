# Objetos da área-vitrine da Aldeia gerados no Tripo (B1v Parte 3, etapa 5): M1 (altar), M9 (poço) e lamparina.
#
# Lê o FBX bruto do Tripo (ArteFonte/.../1_Meshy, fora do Git), solda as costuras, fecha os buracos, põe na escala
# do jogo com o pivô no centro do pé e exporta um FBX por objeto em Assets/_Nibiru/Art/Ambientes/SitioDeUr/Aldeia/
# Objetos, com a textura de cor reduzida ao lado. Cada peça leva o sufixo do material no nome ("Altar_M1__Altar"):
# o menu "Nibiru > Arte > Vitrine da Aldeia > 2. Montar vitrine na cena aberta" dá o material por ele.
#
# Rodar (o caminho do Blender pode mudar), da pasta do projeto:
#   blender -b --factory-startup -P ArteFonte/Ambientes/SitioDeUr/Aldeia/Blender/objetos_vitrine.py -- <pasta do projeto>
#
# Medidas (as do blockout, que ficam como colisor):
#   altar 0,90 (x) x 1,00 (altura) x 0,60 (z), topo plano em y = 1,0 (a tabuleta e a lamparina assentam nele);
#   poço com o anel da base no raio do cilindro do blockout (1,2 m) e o parapeito em 1,0 m de altura;
#   lamparina com 22 cm de comprimento, bico para +z da Unity, pivô no chão embaixo do pavio.
#
# Eixos: o FBX chega na Unity como (x, z, y) do Blender; o +z da Unity é o +y do Blender.

import math
import os
import sys

import bmesh
import bpy
from mathutils import Vector
from mathutils.bvhtree import BVHTree

OUT = "Assets/_Nibiru/Art/Ambientes/SitioDeUr/Aldeia/Objetos"


# ---------- utilidades ----------

def load(path):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=path)
    ob = next(o for o in bpy.data.objects if o.type == 'MESH')
    bpy.context.view_layer.objects.active = ob
    ob.select_set(True)
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return ob


def weld(bm):
    """O Tripo corta a malha nas costuras do UV; soldar mostra só os buracos de verdade (o UV fica por canto)."""
    before = len(bm.verts)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    return before - len(bm.verts)


def boundary_loops(bm):
    left = {e for e in bm.edges if e.is_boundary}
    loops = []
    while left:
        e = left.pop()
        grp, stack = [e], [e]
        while stack:
            x = stack.pop()
            for v in x.verts:
                for y in v.link_edges:
                    if y in left:
                        left.remove(y)
                        grp.append(y)
                        stack.append(y)
        loops.append(grp)
    return loops


def loop_box(edges):
    pts = [v.co for e in edges for v in e.verts]
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return mn, mx


def fill_small_holes(bm, max_size):
    """Fecha buracos pequenos que deixam ver através da peça. Pula os que têm malha logo atrás (o fundo de um
    entalhe, por exemplo): fechar esses taparia o entalhe."""
    bm.normal_update()
    tree = BVHTree.FromBMesh(bm)
    uv = bm.loops.layers.uv.active
    filled = skipped = 0
    for edges in boundary_loops(bm):
        mn, mx = loop_box(edges)
        if (mx - mn).length > max_size:
            continue
        verts = {v for e in edges for v in e.verts}
        c = sum((v.co for v in verts), Vector()) / len(verts)
        n = sum((f.normal for e in edges for f in e.link_faces), Vector())
        if n.length < 1e-9:
            continue
        n.normalize()
        hit = tree.ray_cast(c + n * 0.002, -n, 0.02)
        if hit[0] is not None:
            skipped += 1
            continue
        new = bmesh.ops.holes_fill(bm, edges=edges, sides=0)["faces"]
        new = bmesh.ops.triangulate(bm, faces=new)["faces"]
        if uv:
            for f in new:
                for loop in f.loops:
                    other = [l for l in loop.vert.link_loops if l.face not in new]
                    if other:
                        loop[uv].uv = other[0][uv].uv
        filled += 1
    return filled, skipped


def bounds(bm):
    xs = [v.co.x for v in bm.verts]; ys = [v.co.y for v in bm.verts]; zs = [v.co.z for v in bm.verts]
    return Vector((min(xs), min(ys), min(zs))), Vector((max(xs), max(ys), max(zs)))


def to_pivot(bm, pivot_xy=None):
    """Centro do pé (ou o ponto dado) na origem, com o ponto mais baixo em z = 0."""
    mn, mx = bounds(bm)
    cx, cy = pivot_xy if pivot_xy else ((mn.x + mx.x) / 2, (mn.y + mx.y) / 2)
    for v in bm.verts:
        v.co -= Vector((cx, cy, mn.z))


def report(name, bm):
    mn, mx = bounds(bm)
    loops = boundary_loops(bm)
    big = sorted(((mx_ - mn_).length for mn_, mx_ in (loop_box(l) for l in loops)), reverse=True)[:5]
    print(f"[{name}] verts {len(bm.verts)} faces {len(bm.faces)} tamanho {tuple(round(v, 3) for v in (mx - mn))} "
          f"min {tuple(round(v, 3) for v in mn)} bordas abertas {len(loops)} maiores {[round(b, 3) for b in big]}")


def finish(ob, bm, name):
    bm.normal_update()
    bm.to_mesh(ob.data)
    bm.free()
    ob.data.update()
    ob.name = name
    ob.data.name = name
    return ob


def save_texture(ob, path, size):
    img = next(n.image for n in ob.active_material.node_tree.nodes if n.type == 'TEX_IMAGE' and n.image)
    img.scale(size, size)
    scene = bpy.context.scene
    scene.render.image_settings.file_format = 'JPEG'
    scene.render.image_settings.quality = 90
    img.save_render(path, scene=scene)


def export(path, obs):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in obs:
        ob.select_set(True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_scale_options='FBX_SCALE_UNITS',
                             axis_forward='-Z', axis_up='Y', bake_space_transform=True, mesh_smooth_type='FACE',
                             add_leaf_bones=False, bake_anim=False, object_types={'MESH'}, path_mode='STRIP')


def new_piece(name, bm):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    return ob


# ---------- M1: altar ----------

def altar(src, out_dir):
    ob = load(src)
    bm = bmesh.new(); bm.from_mesh(ob.data)
    print("[altar] soldados", weld(bm))
    report("altar bruto", bm)
    print("[altar] buracos fechados / pulados", fill_small_holes(bm, 0.06))
    to_pivot(bm)
    mn, mx = bounds(bm)
    h = mx.z

    # Perfil da largura por altura: a moldura da base e o tampo saem para fora do corpo.
    for i in range(20):
        z0, z1 = h * i / 20, h * (i + 1) / 20
        xs = [abs(v.co.x) for v in bm.verts if z0 <= v.co.z < z1]
        print(f"[altar] perfil z {z0:.3f}-{z1:.3f} meia-largura {max(xs) if xs else 0:.3f}")

    # Altura: o bloco do Tripo é baixo (0,57 da largura). Para chegar a 1,0 m sem esticar a inscrição e as
    # molduras, a moldura da base e a faixa de cima (inscrição e tampo) crescem pouco, e o corpo liso estica.
    sx, sy = 0.9 / (mx.x - mn.x), 0.6 / (mx.y - mn.y)
    base_top, frieze_bottom, k = BASE_TOP * h, FRIEZE_BOTTOM * h, DETAIL_GROWTH
    lower = base_top * k
    upper = (h - frieze_bottom) * k
    body = (1.0 - lower - upper) / (frieze_bottom - base_top)
    print(f"[altar] escala x {sx:.3f} z(Unity) {sy:.3f}; altura: base e friso x{k:.2f}, corpo liso x{body:.2f}")

    def remap(z):
        if z <= base_top:
            return z * k
        if z <= frieze_bottom:
            return lower + (z - base_top) * body
        return lower + (frieze_bottom - base_top) * body + (z - frieze_bottom) * k

    for v in bm.verts:
        v.co = Vector((v.co.x * sx, v.co.y * sy, remap(v.co.z)))

    # Topo plano em 1,0: o que está a menos de 1,5 cm do topo e olha para cima desce ou sobe para 1,0.
    bm.normal_update()
    flat = 0
    for v in bm.verts:
        if v.co.z > 1.0 - 0.015 and v.normal.z > 0.5:
            v.co.z = 1.0
            flat += 1
    mn, mx = bounds(bm)
    for v in bm.verts:
        v.co.z = min(v.co.z, 1.0)
    print(f"[altar] topo achatado: {flat} vértices; topo antes {mx.z:.4f}")
    report("altar final", bm)
    finish(ob, bm, "Altar_M1__Altar")
    save_texture(ob, os.path.join(out_dir, "Altar_M1_cor.jpg"), 2048)
    export(os.path.join(out_dir, "Altar_M1.fbx"), [ob])


# Frações da altura original do Tripo (conferidas pelo perfil impresso): topo da moldura da base e pé da inscrição.
BASE_TOP = 0.24
FRIEZE_BOTTOM = 0.52
DETAIL_GROWTH = 1.2


# ---------- M9: poço ----------

def poco(src, out_dir):
    ob = load(src)
    bm = bmesh.new(); bm.from_mesh(ob.data)
    print("[poço] soldados", weld(bm))
    report("poço bruto", bm)
    print("[poço] buracos fechados / pulados", fill_small_holes(bm, 0.05))
    mn, mx = bounds(bm)
    zmin = mn.z

    # Centro do anel: média das pontas do anel da base.
    base = [v.co for v in bm.verts if v.co.z < zmin + 0.04]
    cx = (min(p.x for p in base) + max(p.x for p in base)) / 2
    cy = (min(p.y for p in base) + max(p.y for p in base)) / 2
    to_pivot(bm, (cx, cy))
    rad = lambda p: math.hypot(p.x, p.y)
    r_base = max(rad(v.co) for v in bm.verts if v.co.z < 0.04)

    # Buracos grandes: a faixa de baixo da parede de fora (onde o reboco descascou, o Tripo não fez o tijolo)
    # e o fundo do poço, aberto. Acha as duas bordas.
    loops = [(l, loop_box(l)) for l in boundary_loops(bm)]
    loops.sort(key=lambda x: -len(x[0]))
    for l, (a, b) in loops[:6]:
        vs = {v for e in l for v in e.verts}
        print(f"[poço] borda {len(l)} arestas z {a.z:.3f}-{b.z:.3f} raio {min(rad(v.co) for v in vs):.3f}-{max(rad(v.co) for v in vs):.3f}")
    wall_loop = next(l for l, (a, b) in loops if (b.z - a.z) > 0.08)
    wall_verts = {v for e in wall_loop for v in e.verts}
    inner_loop = min((l for l, (a, b) in loops if (b.z - a.z) < 0.01 and len(l) > 40),
                     key=lambda l: max(rad(v.co) for e in l for v in e.verts))
    inner_verts = {v for e in inner_loop for v in e.verts}
    r_inner = max(rad(v.co) for v in inner_verts)
    z_inner = sum(v.co.z for v in inner_verts) / len(inner_verts)
    z_wall_top = max(v.co.z for v in wall_verts)

    # Raio da parede de fora em cada ângulo (pela borda do reboco), 4 mm para dentro.
    seg = 96
    radii = []
    for i in range(seg):
        a = 2 * math.pi * i / seg
        near = sorted(wall_verts, key=lambda v: abs(math.atan2(math.sin(math.atan2(v.co.y, v.co.x) - a), math.cos(math.atan2(v.co.y, v.co.x) - a))))[:4]
        radii.append(sum(rad(v.co) for v in near) / len(near) - 0.004)

    # Altura do parapeito: o ponto mais alto da parede (entre os raios de dentro e de fora), longe dos dois postes,
    # que ficam no eixo x.
    r_out = max(radii)
    parapet = max(v.co.z for v in bm.verts
                  if r_inner * 0.9 < rad(v.co) < r_out * 1.05 and abs(v.co.y) > 0.7 * rad(v.co) and v.co.z < 0.7)
    s = 1.2 / r_base
    sz = min(1.2, max(0.8, 1.0 / (parapet * s)))
    print(f"[poço] raio da base {r_base:.3f}, parapeito {parapet:.3f}, escala {s:.3f}, altura x{sz:.3f} "
          f"(parapeito {parapet * s * sz:.3f} m, total {mx.z * s * sz:.3f} m)")
    for v in bm.verts:
        v.co = Vector((v.co.x * s, v.co.y * s, v.co.z * s * sz))
    report("poço final", bm)
    finish(ob, bm, "Poco_M9__Poco")
    save_texture(ob, os.path.join(out_dir, "Poco_M9_cor.jpg"), 2048)

    # Faixa de tijolo atrás do reboco descascado (material da Aldeia, UV em metros).
    z0, z1 = (z_inner - 0.01) * s * sz, (z_wall_top + 0.015) * s * sz
    b2 = bmesh.new()
    uv = b2.loops.layers.uv.new()
    ring = [(b2.verts.new((math.cos(2 * math.pi * i / seg) * r * s, math.sin(2 * math.pi * i / seg) * r * s, z0)),
             b2.verts.new((math.cos(2 * math.pi * i / seg) * r * s, math.sin(2 * math.pi * i / seg) * r * s, z1)))
            for i, r in enumerate(radii)]
    length = 0.0
    for i in range(seg):
        (a0, a1), (b0, b1) = ring[i], ring[(i + 1) % seg]
        step = (b0.co - a0.co).length
        f = b2.faces.new((a0, b0, b1, a1))
        for loop, (u, v) in zip(f.loops, ((length, z0), (length + step, z0), (length + step, z1), (length, z1))):
            loop[uv].uv = (u, v)
        length += step
    bmesh.ops.recalc_face_normals(b2, faces=b2.faces)
    b2.normal_update()
    if b2.faces[0].normal.dot(Vector((b2.faces[0].calc_center_median().x, b2.faces[0].calc_center_median().y, 0))) < 0:
        bmesh.ops.reverse_faces(b2, faces=b2.faces)
    bmesh.ops.triangulate(b2, faces=b2.faces)
    brick = new_piece("Poco_M9_Base__TijoloPoco", b2)

    # Água escura no fundo, fechando a boca de baixo do revestimento de dentro.
    b3 = bmesh.new()
    uv = b3.loops.layers.uv.new()
    zw = z_inner * s * sz + 0.003
    rw = (r_inner + 0.01) * s
    center = b3.verts.new((0, 0, zw))
    rim = [b3.verts.new((math.cos(2 * math.pi * i / 48) * rw, math.sin(2 * math.pi * i / 48) * rw, zw)) for i in range(48)]
    for i in range(48):
        f = b3.faces.new((center, rim[i], rim[(i + 1) % 48]))
        for loop in f.loops:
            loop[uv].uv = (loop.vert.co.x, loop.vert.co.y)
    water = new_piece("Poco_M9_Agua__Agua", b3)
    print(f"[poço] faixa de tijolo z {z0:.3f}-{z1:.3f}; água em z {zw:.3f}, raio {rw:.3f}")
    export(os.path.join(out_dir, "Poco_M9.fbx"), [ob, brick, water])


# ---------- lamparina ----------

def lamparina(src, out_dir):
    ob = load(src)
    bm = bmesh.new(); bm.from_mesh(ob.data)
    print("[lamparina] soldados", weld(bm))
    report("lamparina bruta", bm)
    print("[lamparina] buracos fechados / pulados", fill_small_holes(bm, 0.05))
    mn, mx = bounds(bm)
    s = 0.22 / (mx.y - mn.y)
    # O bico do Tripo aponta para -y do Blender; gira 180 graus para ele apontar para +z da Unity.
    for v in bm.verts:
        v.co = Vector((-v.co.x * s, -v.co.y * s, v.co.z * s))
    to_pivot(bm)
    # Pavio: o ponto mais alto da boca do bico (a parte da frente, os 30% de cima no comprimento).
    mn, mx = bounds(bm)
    front = [v.co for v in bm.verts if v.co.y > mx.y - 0.3 * (mx.y - mn.y)]
    tip = max(front, key=lambda p: p.z)
    spout = [p for p in front if p.z > tip.z - 0.006]
    wick = sum(spout, Vector()) / len(spout)
    print(f"[lamparina] pavio em x {wick.x:.4f} z(Unity) {wick.y:.4f} altura {wick.z:.4f}")
    for v in bm.verts:
        v.co -= Vector((wick.x, wick.y, 0))
    report("lamparina final", bm)
    finish(ob, bm, "Lamparina__Lamparina")
    save_texture(ob, os.path.join(out_dir, "Lamparina_cor.jpg"), 1024)
    export(os.path.join(out_dir, "Lamparina.fbx"), [ob])


def main():
    root = sys.argv[sys.argv.index("--") + 1]
    out_dir = os.environ.get("OBJETOS_SAIDA") or os.path.join(root, OUT)  # outra pasta, para conferir antes
    os.makedirs(out_dir, exist_ok=True)
    src = os.path.join(root, "ArteFonte")
    only = sys.argv[sys.argv.index("--") + 2:]
    jobs = {
        "altar": (altar, "Ambientes/Altar/1_Meshy/Altar_M1.fbx"),
        "poco": (poco, "Ambientes/PocoM9/1_Meshy/Poço.fbx"),
        "lamparina": (lamparina, "Objetos/LamparinaDeBarro/1_Meshy/Lamparina.fbx"),
    }
    for name, (fn, path) in jobs.items():
        if not only or name in only:
            fn(os.path.join(src, path), out_dir)


main()
