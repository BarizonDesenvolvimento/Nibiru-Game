# Procura a melhor posição rígida de uma peça sobre o corpo, para algumas escalas fixas.
# Custo = profundidade média com que o corpo atravessa a peça (medida por raios saindo do eixo do corpo).
# Manto: inclinação (rx) e deslocamento (dy, dz) livres; no fim desce a peça até ela "pousar" nos ombros.
# Saia: borda de cima presa na cintura (CINTURA) e barra perto do chão (BARRA); livres rx, dy e a
#       escala horizontal s.
# Imprime uma tabela por escala; não salva nada.
# Uso: blender -b Ninsar_Final.blend -P Scripts/otimizar_encaixe.py -- <Manto|Saia>
import bpy, sys, math
from collections import defaultdict
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

CINTURA, BARRA = 1.25, 0.02
peca = sys.argv[sys.argv.index("--") + 1]
dg = bpy.context.evaluated_depsgraph_get()

def malha_mundo(nome):
    o = bpy.data.objects[nome]
    me = o.evaluated_get(dg).to_mesh()
    return [o.matrix_world @ v.co for v in me.vertices], [tuple(p.vertices) for p in me.polygons]

corpo, _ = malha_mundo("Ninsar_Corpo")
verts, faces = malha_mundo("Ninsar_" + peca)
bvh = BVHTree.FromPolygons(verts, faces)
zmin, zmax = min(v.z for v in verts), max(v.z for v in verts)

camadas = defaultdict(list)
for p in corpo:
    if abs(p.x) < 0.22:
        camadas[round(p.z / 0.05)].append(p.y)
centro_y = {k: sum(v) / len(v) for k, v in camadas.items()}

if peca == "Manto":
    pts = [p for p in corpo[::2] if abs(p.x) < 0.24 and 0.85 < p.z < 1.68]  # sem a cabeça: o capuz se ajusta depois
    pivo = Vector((0.0, 0.05, 1.55))          # entre os ombros
    escalas = [1.03]
    livres = {"rx": 4.0, "dy": 0.02, "dz": 0.02}
else:
    pts = [p for p in corpo[::2] if 0.2 < p.z < CINTURA + 0.03]
    pivo = Vector((0.0, -0.04, zmax))         # borda de cima da saia
    escalas = [1.08]
    livres = {"rx": 4.0, "dy": 0.02}
raios = [(p, Vector((0.0, centro_y.get(round(p.z / 0.05), 0.0), p.z))) for p in pts]

def matriz(pr):
    if peca == "Manto":
        sz, dz = pr["s"], pr["dz"]
    else:
        sz = (CINTURA - BARRA) / (zmax - zmin)
        dz = CINTURA - zmax
    return (Matrix.Translation((0, pr["dy"], dz)) @ Matrix.Translation(pivo)
            @ Matrix.Rotation(math.radians(pr["rx"]), 4, "X")
            @ Matrix.Diagonal((pr["s"], pr["s"], sz, 1)) @ Matrix.Translation(-pivo))

def custo(pr):
    inv = matriz(pr).inverted()
    soma, n = 0.0, 0
    for p, o in raios:
        q, oo = inv @ p, inv @ o
        d = q - oo
        L = d.length
        if L < 1e-6:
            continue
        h = bvh.ray_cast(oo, d / L, L)
        if h[0] is not None:
            soma += (q - h[0]).length
            n += 1
    return soma / len(raios) * 1000.0, n   # mm médios, número de pontos furando

def otimizar(s):
    params = {"rx": 0.0, "dy": 0.06 if peca == "Manto" else 0.04, "dz": 0.0, "s": s}
    passos = dict(livres)
    atual, furos = custo(params)
    for _ in range(4):
        for k in passos:
            melhorou = True
            while melhorou:
                melhorou = False
                for sinal in (1, -1):
                    tent = dict(params)
                    tent[k] += sinal * passos[k]
                    c, f = custo(tent)
                    if c < atual - 1e-4:
                        params, atual, furos, melhorou = tent, c, f, True
                        break
        passos = {k: v / 2 for k, v in passos.items()}
    if peca == "Manto":   # pousar: desce enquanto o custo não piora mais que 2%
        base = atual
        while True:
            tent = dict(params)
            tent["dz"] -= 0.005
            c, f = custo(tent)
            if c > base * 1.02 + 0.01:
                break
            params, atual, furos = tent, c, f
    return params, atual, furos

for s in escalas:
    params, c, f = otimizar(s)
    print(f"ESCALA {s:.2f} {({k: round(v, 4) for k, v in params.items()})} custo={c:.3f}mm furos={f}/{len(raios)}")
    print("MATRIZ", s, [[round(x, 6) for x in r] for r in matriz(params)])
