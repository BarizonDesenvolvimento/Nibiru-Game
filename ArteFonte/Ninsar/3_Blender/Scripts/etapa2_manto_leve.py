# Etapa 2a: versão leve do manto (Ninsar_Manto_Leve), malha uniforme de quadrados com uma face só.
# O manto do Meshy é uma casca fechada e fina (~2 mm): duas faces quase coladas. Aqui:
# 1. engrossa o pano e refaz como casca limpa (voxel); separar a face de fora direto do original
#    deixa a folha cheia de furos, porque as duas faces estão coladas demais;
# 2. QuadriFlow na casca (dois lados) e apaga o lado de dentro (normal virada para o corpo), com
#    votação entre vizinhos para limpar o ruído nas bordas;
# 3. suaviza de leve e encosta de volta na superfície original (shrinkwrap).
# Uso: blender -b Ninsar_Final.blend -P Scripts/etapa2_manto_leve.py [-- faces]
import bpy, bmesh, sys
from mathutils import Vector
from mathutils.bvhtree import BVHTree

FACES = int(sys.argv[sys.argv.index("--") + 1]) if "--" in sys.argv else 8000
NOME = "Ninsar_Manto_Leve"

alto = bpy.data.objects["Ninsar_Manto"]
corpo = bpy.data.objects["Ninsar_Corpo"]
if NOME in bpy.data.objects:
    bpy.data.objects.remove(bpy.data.objects[NOME])

dg = bpy.context.evaluated_depsgraph_get()
me_c = corpo.evaluated_get(dg).to_mesh()
bvh_corpo = BVHTree.FromPolygons([corpo.matrix_world @ v.co for v in me_c.vertices],
                                 [tuple(p.vertices) for p in me_c.polygons])

# 1. Casca limpa: engrossa o pano (cada face anda ESPESSURA para fora dela mesma, então as duas
#    faces se afastam) e refaz por voxel, que fecha furinhos e junta as franjas.
ESPESSURA, VOXEL = 0.010, 0.007
leve = alto.copy()
leve.data = alto.data.copy()
leve.name = leve.data.name = NOME
leve.modifiers.clear()
bpy.context.scene.collection.objects.link(leve)
leve.data.materials.clear()
for o in bpy.context.view_layer.objects:
    o.select_set(False)
leve.select_set(True)
bpy.context.view_layer.objects.active = leve
d = leve.modifiers.new("Engrossar", "DISPLACE")
d.direction, d.strength, d.mid_level = "NORMAL", ESPESSURA, 0.0
bpy.ops.object.modifier_apply(modifier=d.name)
leve.data.remesh_voxel_size = VOXEL
bpy.ops.object.voxel_remesh()
print("VOXEL", len(leve.data.polygons), "faces")

# O voxel deixa alguns pontos onde o pano encosta nele mesmo (não-manifold); o QuadriFlow recusa.
# Apaga as faces em volta desses pontos e fecha os furinhos de novo, até a casca ficar limpa.
bm = bmesh.new()
bm.from_mesh(leve.data)
for rodada in range(6):
    ruins = {f for v in bm.verts if not v.is_manifold for f in v.link_faces}
    ruins |= {f for e in bm.edges if not e.is_manifold for f in e.link_faces}
    print(f"  limpeza {rodada}: {len(ruins)} faces em pontos ruins")
    if not ruins:
        break
    bmesh.ops.delete(bm, geom=list(ruins), context="FACES")
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context="VERTS")
    bmesh.ops.holes_fill(bm, edges=[e for e in bm.edges if e.is_boundary], sides=0)
    bmesh.ops.triangulate(bm, faces=[f for f in bm.faces if len(f.verts) > 4])
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
bm.to_mesh(leve.data)
bm.free()

# 2. QuadriFlow na casca fechada (os dois lados), depois apaga o lado de dentro
r = bpy.ops.object.quadriflow_remesh(target_faces=FACES * 2, use_mesh_symmetry=False, seed=0)
print("QUADRIFLOW", r, len(leve.data.polygons), "faces")
bm = bmesh.new()
bm.from_mesh(leve.data)
bm.transform(leve.matrix_world)
bm.normal_update()
bm.faces.ensure_lookup_table()
fora = []
for f in bm.faces:
    c = f.calc_center_median()
    loc, _, _, _ = bvh_corpo.find_nearest(c)
    fora.append(1.0 if f.normal.dot(loc - c) < 0 else 0.0)
vizinhos = [[g.index for e in f.edges for g in e.link_faces if g is not f] for f in bm.faces]
for _ in range(4):
    fora = [1.0 if (sum(fora[j] for j in viz) + fora[i]) / (len(viz) + 1) > 0.5 else 0.0
            for i, viz in enumerate(vizinhos)]
bmesh.ops.delete(bm, geom=[f for f, k in zip(bm.faces, fora) if k == 0.0], context="FACES")
bm.faces.ensure_lookup_table()
visto, ilhas = set(), []
for f in bm.faces:
    if f.index in visto:
        continue
    pilha, ilha = [f], []
    visto.add(f.index)
    while pilha:
        a = pilha.pop()
        ilha.append(a)
        for e in a.edges:
            for b in e.link_faces:
                if b.index not in visto:
                    visto.add(b.index)
                    pilha.append(b)
    ilhas.append(ilha)
ilhas.sort(key=len, reverse=True)
print("ILHAS do lado de fora:", [len(i) for i in ilhas[:6]], "total", len(ilhas))
bmesh.ops.delete(bm, geom=[f for ilha in ilhas[1:] if len(ilha) < 50 for f in ilha], context="FACES")
bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context="VERTS")
bm.transform(leve.matrix_world.inverted())
bm.to_mesh(leve.data)
bm.free()

# 3. Suaviza e encosta na superfície original
sm = leve.modifiers.new("Suavizar", "SMOOTH")
sm.factor, sm.iterations = 0.5, 3
sw = leve.modifiers.new("Encostar", "SHRINKWRAP")
sw.target, sw.wrap_method, sw.wrap_mode, sw.offset = alto, "NEAREST_SURFACEPOINT", "ABOVE_SURFACE", 0.002
for m in list(leve.modifiers):
    bpy.ops.object.modifier_apply(modifier=m.name)
for p in leve.data.polygons:
    p.use_smooth = True

bm = bmesh.new()
bm.from_mesh(leve.data)
lados = {}
for f in bm.faces:
    lados[len(f.verts)] = lados.get(len(f.verts), 0) + 1
print("LEVE", NOME, "faces", len(bm.faces), "por lados", lados, "verts", len(bm.verts),
      "bordas", sum(1 for e in bm.edges if e.is_boundary))
bm.free()
bpy.ops.wm.save_mainfile(compress=True)
print("SALVO")
