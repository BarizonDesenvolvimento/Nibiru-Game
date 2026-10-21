# Analisa a malha de uma peça de pano: partes soltas, bordas abertas, espessura e lados (dentro/fora).
# Uso: blender -b Ninsar_Final.blend -P Scripts/analisar_pano.py -- Ninsar_Manto
import bpy, bmesh, sys, random
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

nome = sys.argv[sys.argv.index("--") + 1]
o = bpy.data.objects[nome]
bm = bmesh.new()
bm.from_mesh(o.data)
bm.transform(o.matrix_world)
bm.normal_update()

abertas = sum(1 for e in bm.edges if e.is_boundary)
nao_manifold = sum(1 for e in bm.edges if not e.is_manifold)
# partes soltas
bm.verts.ensure_lookup_table()
visto, partes = set(), []
for v in bm.verts:
    if v.index in visto:
        continue
    pilha, n = [v], 0
    visto.add(v.index)
    while pilha:
        a = pilha.pop()
        n += 1
        for e in a.link_edges:
            b = e.other_vert(a)
            if b.index not in visto:
                visto.add(b.index)
                pilha.append(b)
    partes.append(n)
partes.sort(reverse=True)
print(f"PANO {nome}: verts={len(bm.verts)} faces={len(bm.faces)} bordas_abertas={abertas} "
      f"nao_manifold={nao_manifold} partes={len(partes)} maiores={partes[:8]}")

# espessura: raio para dentro (contra a normal) a partir de faces sorteadas
bvh = BVHTree.FromBMesh(bm)
bm.faces.ensure_lookup_table()
amostra = random.Random(1).sample(list(bm.faces), 3000)
esp = []
for f in amostra:
    c = f.calc_center_median()
    h = bvh.ray_cast(c - f.normal * 1e-4, -f.normal, 0.2)
    esp.append(h[3] if h[0] is not None else np.nan)
esp = np.array(esp)
ok = esp[~np.isnan(esp)]
print(f"ESPESSURA: acertos {len(ok)}/{len(esp)}; percentis 10/50/90 = "
      f"{np.percentile(ok, 10) * 1000:.1f} / {np.percentile(ok, 50) * 1000:.1f} / {np.percentile(ok, 90) * 1000:.1f} mm")
areas = np.array([f.calc_area() for f in bm.faces])
print(f"ARESTA média aprox {np.sqrt(areas.mean() * 2) * 1000:.1f} mm; área total {areas.sum():.2f} m²")
