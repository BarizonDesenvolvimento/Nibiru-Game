# Etapa 2 (manto): cria um manto novo e limpo para a Ninsar, como um molde de roupa, e veste com
# simulação de tecido. O manto do Meshy vira só referência (Ref_Manto_Meshy, escondido).
#
# Molde (malha de quadrados, uma face só):
# - Corpo: colunas que descem do pescoço até o tornozelo, abertas na frente, com uma "pala" por cima
#   dos ombros e cavas retangulares onde os braços saem.
# - Capuz: grade em volta da cabeça, com a abertura do rosto na frente; a fileira de trás dobra sobre
#   ela mesma (costura das costas do capuz). A primeira coluna do capuz é a própria gola do corpo.
# UVs (feitas no molde, antes da simulação, em metros):
# - material Manto_Tecido: tecido repetível (Texturas/Processadas/Manto_Tecido_*).
# - material Manto_Faixa: faixa de bordado nas bordas da frente (continua pela borda do capuz), na
#   barra, nas laterais (abaixo das cavas) e no centro das costas. U = ao longo da faixa, V = 0..1 de
#   uma linha dourada à outra.
# Simulação: corpo e uma versão simplificada da saia como colisores; gola e pala presas.
# Uso: blender -b Ninsar_Final.blend -P Scripts/etapa2_manto_novo.py
import bpy, bmesh, math
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

# --- Ajustes ---
NV_H = 36            # quadrados do capuz da frente até a nuca (de cada lado); o corpo tem 2*NV_H colunas
NU_H = 64            # quadrados do capuz em volta do rosto (par)
NY = 5               # fileiras da pala (do pescoço até a linha dos ombros)
NM = 58              # fileiras do corpo, da linha dos ombros até a barra
BARRA = 0.12         # altura da barra no molde; depois de assentar fica perto do tornozelo
FRENTE = 172.0       # ângulo da borda da frente (0 = costas, 180 = frente)
FOLGA = 0.04         # folga inicial do corpo do manto sobre corpo/saia
EVASE = 0.04         # quanto o manto abre por metro de queda (forma inicial; a simulação assenta)
CAPUZ_FOLGA, CAPUZ_FOLGA_ROSTO = 0.025, 0.04
CALOTA = 0.8        # o alto do capuz (pontos acima deste ângulo, visto do centro da cabeça) fica preso
GOLA_FRENTE = 165.0  # ângulo da gola na borda da frente
BUSTO = 1.30         # a borda da frente fica presa até esta altura (como um forro costurado)
CAVA_BAIXO, CAVA_MEIA_LARGURA = 1.37, 30.0   # base da cava (m) e meia-largura (graus)
TECIDO_L = 0.45                               # largura de uma repetição do tecido (m)
TECIDO_A = TECIDO_L * 1140 / 1045             # altura (a textura tem 1045 x 1140 px)
FAIXA_LARG = 0.085                            # largura de referência da faixa (m)
FAIXA_REP = FAIXA_LARG * 1085 / 415           # um ciclo do bordado ao longo da faixa (m)
K_FRENTE, K_BARRA, K_COSTAS, K_LADO, K_ROSTO = 3, 4, 2, 2, 8   # larguras das faixas (em quadrados)
QUADROS = 120
# ----------------

cena = bpy.context.scene
corpo = bpy.data.objects["Ninsar_Corpo"]
saia = bpy.data.objects["Ninsar_Saia"]
if "Ref_Manto_Meshy" not in bpy.data.objects:
    velho = bpy.data.objects["Ninsar_Manto"]
    velho.name = "Ref_Manto_Meshy"
else:
    velho = bpy.data.objects["Ref_Manto_Meshy"]
    if "Ninsar_Manto" in bpy.data.objects:
        bpy.data.objects.remove(bpy.data.objects["Ninsar_Manto"])
velho.hide_render = velho.hide_viewport = True

dg = bpy.context.evaluated_depsgraph_get()
def malha(o):
    me = o.evaluated_get(dg).to_mesh()
    vs = np.array([(o.matrix_world @ v.co)[:] for v in me.vertices])
    fs = [tuple(p.vertices) for p in me.polygons]
    return vs, fs
cv, cf = malha(corpo)
bvh_c = BVHTree.FromPolygons([Vector(v) for v in cv], cf)
sv, _ = malha(saia)

# Eixo do corpo e centro da cabeça
tronco = cv[np.abs(cv[:, 0]) < 0.22]
alturas = np.arange(0, 2.0, 0.05)
centros = np.array([tronco[np.abs(tronco[:, 2] - z) < 0.025, 1].mean()
                    if (np.abs(tronco[:, 2] - z) < 0.025).any() else 0.0 for z in alturas])
def eixo_y(z):
    return np.interp(z, alturas, centros)
cab = cv[(cv[:, 2] > 1.70) & (np.abs(cv[:, 0]) < 0.12)]
C = Vector(((cab[:, 0].min() + cab[:, 0].max()) / 2, (cab[:, 1].min() + cab[:, 1].max()) / 2, 1.78))

# Envelope (raio máximo por ângulo e altura) do corpo sem os braços + saia
pts = np.vstack([cv[~((np.abs(cv[:, 0]) > 0.20) & (cv[:, 2] > 1.30))], sv])
DZ, DA = 0.03, 4
psi_p = np.degrees(np.arctan2(pts[:, 0], pts[:, 1] - eixo_y(pts[:, 2]))) % 360
r_p = np.hypot(pts[:, 0], pts[:, 1] - eixo_y(pts[:, 2]))
env = np.zeros((360 // DA, int(2.0 / DZ) + 1))
np.maximum.at(env, ((psi_p // DA).astype(int) % (360 // DA), (pts[:, 2] / DZ).astype(int).clip(0, env.shape[1] - 1)), r_p)
env = np.maximum(env, np.maximum(np.roll(env, 1, 0), np.roll(env, -1, 0)))
def envelope(psi, z):
    return env[int((psi % 360) // DA), int(np.clip(z / DZ, 0, env.shape[1] - 1))]

braco = cv[(cv[:, 0] > 0.22) & (cv[:, 2] > 1.35)]
psi_braco = math.degrees(math.atan2(0.22, braco[:, 1].mean() - eixo_y(1.5)))
print("CABECA", tuple(round(x, 3) for x in C), "braço a", round(psi_braco, 1), "graus")

def raio_corpo(origem, d, padrao=0.18):
    h = bvh_c.ray_cast(origem, d, 1.0)
    return h[3] if h[0] is not None else padrao

def empurrar_ponto(p, folga=0.015):
    for _ in range(4):
        loc, nor, _, _ = bvh_c.find_nearest(p, 0.3)
        if loc is None:
            return p
        s = (p - loc).dot(nor)
        if s >= folga:
            return p
        p = p + nor * (folga - s)
    return p

# ---------- Capuz ----------
def esf(phi, theta):
    p, t = math.radians(phi), math.radians(theta)
    return Vector((math.sin(p) * math.cos(t), math.cos(p) * math.cos(t), math.sin(t)))
def bezier(a, b, c, s):
    return tuple((1 - s) ** 2 * a[i] + 2 * (1 - s) * s * b[i] + s * s * c[i] for i in range(2))
def espelho(v):
    return Vector((-v.x, v.y, v.z))
TETA_GOLA = -58.0
def F(u):   # borda do rosto: do pescoço (frente, esquerda) passando ao lado do rosto até o alto da testa
    s = 2 * u if u <= 0.5 else 2 * (1 - u)
    d = esf(*bezier((GOLA_FRENTE, TETA_GOLA), (80, 5), (180, 55), s))
    return d if u <= 0.5 else espelho(d)
def B(u):   # costura de trás: da nuca até o alto de trás da cabeça (dobrada)
    s = 2 * u if u <= 0.5 else 2 * (1 - u)
    b = math.radians(TETA_GOLA + (30 - TETA_GOLA) * s)
    return Vector((0.0, math.cos(b), math.sin(b)))
def L(v):   # gola esquerda: da frente até a nuca
    return esf(GOLA_FRENTE * (1 - v), TETA_GOLA)
def R(v):
    return espelho(L(v))
P00, P10, P01, P11 = F(0), F(1), B(0), B(1)
def capuz_dir(u, v):
    d = ((1 - v) * F(u) + v * B(u) + (1 - u) * L(v) + u * R(v)
         - ((1 - u) * (1 - v) * P00 + u * (1 - v) * P10 + (1 - u) * v * P01 + u * v * P11))
    return d.normalized()

pos = {}
for i in range(NU_H + 1):
    for j in range(NV_H + 1):
        u, v = i / NU_H, j / NV_H
        d = capuz_dir(u, v)
        folga = CAPUZ_FOLGA + (CAPUZ_FOLGA_ROSTO - CAPUZ_FOLGA) * (1 - v) ** 2
        pos[("h", i, j)] = C + d * (raio_corpo(C, d) + folga)

def chave(tipo, a, b):
    """Junta vértices que são o mesmo ponto: gola do capuz = topo do corpo; fileira de trás dobrada."""
    if tipo == "h":
        i, j = a, b
        if j == NV_H and i > NU_H // 2:
            i = NU_H - i
        if i == 0:
            return ("b", j, 0)
        if i == NU_H:
            return ("b", 2 * NV_H - j, 0)
        return ("h", i, j)
    return ("b", a, b)

# ---------- Corpo ----------
NU = 2 * NV_H
def psi_coluna(k):
    return FRENTE * (1 - k / NV_H) if k <= NV_H else -FRENTE * (k / NV_H - 1)
def z_ombro(psi):
    d = min(abs(psi - psi_braco), abs(-psi - psi_braco))
    return 1.46 + (1.60 - 1.46) * math.exp(-(d / 50.0) ** 2)
def ponto_cilindro(psi, z, r):
    a = math.radians(psi)
    return Vector((r * math.sin(a), float(eixo_y(z)) + r * math.cos(a), z))

for k in range(NU + 1):
    if k <= NV_H:
        pos[("b", k, 0)] = pos[("h", 0, k)]
    else:
        pos[("b", k, 0)] = pos[("h", NU_H, NU - k)]
    psi = psi_coluna(k)
    zs = z_ombro(psi)
    S = empurrar_ponto(ponto_cilindro(psi, zs, envelope(psi, zs) + 0.02), 0.02)
    T = pos[("b", k, 0)]
    for r in range(1, NY + 1):
        t = r / NY
        pos[("b", k, r)] = empurrar_ponto(T.lerp(S, t) if r < NY else S, 0.015)
    col = env[int((psi % 360) // DA)]
    for m in range(1, NM + 1):
        z = zs + (BARRA - zs) * m / NM
        maxr = col[int(z / DZ): int(zs / DZ) + 1].max()
        p = ponto_cilindro(psi, z, maxr + FOLGA + EVASE * (zs - z))
        pos[("b", k, NY + m)] = empurrar_ponto(p, 0.015)
NR = NY + NM

# A cava é um retângulo limpo na grade: da linha dos ombros até a fileira que, na coluna do braço,
# chega à altura CAVA_BAIXO (assim a borda não fica em degraus)
k_braco = int(round(NV_H - (FRENTE - psi_braco) / FRENTE * NV_H))
r_cava_base = next(r for r in range(NY, NR + 1) if pos[("b", k_braco, r)].z < CAVA_BAIXO)
def na_cava(k, r):
    psi = psi_coluna(k + 0.5)
    d = min(abs(psi - psi_braco), abs(-psi - psi_braco))
    return NY <= r < r_cava_base and d < CAVA_MEIA_LARGURA

# ---------- Faces, materiais e UVs ----------
bm = bmesh.new()
uv = bm.loops.layers.uv.new("UVMap")
verts = {}
def vert(c):
    if c not in verts:
        verts[c] = bm.verts.new(pos[c])
    return verts[c]

# comprimentos no molde (para UV em metros)
def comprimentos_corpo():
    U, V = {}, {}
    for r in range(NR + 1):
        acc = 0.0
        U[(NV_H, r)] = 0.0
        for k in range(NV_H + 1, NU + 1):
            acc += (pos[("b", k, r)] - pos[("b", k - 1, r)]).length
            U[(k, r)] = acc
        acc = 0.0
        for k in range(NV_H - 1, -1, -1):
            acc -= (pos[("b", k, r)] - pos[("b", k + 1, r)]).length
            U[(k, r)] = acc
    for k in range(NU + 1):
        acc = 0.0
        V[(k, 0)] = 0.0
        for r in range(1, NR + 1):
            acc += (pos[("b", k, r)] - pos[("b", k, r - 1)]).length
            V[(k, r)] = acc
    return U, V
UB, VB = comprimentos_corpo()

def comprimentos_capuz():
    U, V = {}, {}
    for j in range(NV_H + 1):
        acc = 0.0
        U[(NU_H // 2, j)] = 0.0
        for i in range(NU_H // 2 + 1, NU_H + 1):
            acc += (pos[("h", i, j)] - pos[("h", i - 1, j)]).length
            U[(i, j)] = acc
        acc = 0.0
        for i in range(NU_H // 2 - 1, -1, -1):
            acc -= (pos[("h", i, j)] - pos[("h", i + 1, j)]).length
            U[(i, j)] = acc
    for i in range(NU_H + 1):
        acc = 0.0
        V[(i, 0)] = 0.0
        for j in range(1, NV_H + 1):
            acc += (pos[("h", i, j)] - pos[("h", i, j - 1)]).length
            V[(i, j)] = acc
    return U, V
UH, VH = comprimentos_capuz()

r_cava_fim = max(r for k in range(NU) for r in range(NR) if na_cava(k, r)) + 1
colunas_lado = [int(round(NV_H - (FRENTE - psi_braco) / FRENTE * NV_H)),
                int(round(NV_H + (FRENTE - psi_braco) / FRENTE * NV_H))]
def faixa_corpo(k, r):
    """Qual faixa (se alguma) cobre o quadrado (k, r) do corpo, e as UVs dos 4 cantos."""
    cantos = [(k, r), (k + 1, r), (k + 1, r + 1), (k, r + 1)]
    if k < K_FRENTE or k >= NU - K_FRENTE:            # bordas da frente: U ao longo, V através
        borda = 0 if k < K_FRENTE else NU
        return [(VB[(borda, rr)] / FAIXA_REP, abs(kk - borda) / K_FRENTE) for kk, rr in cantos]
    if r >= NR - K_BARRA:                             # barra
        return [(UB[(kk, NR)] / FAIXA_REP, (NR - rr) / K_BARRA) for kk, rr in cantos]
    for centro in (NV_H - (FRENTE - psi_braco) / FRENTE * NV_H, NV_H + (FRENTE - psi_braco) / FRENTE * NV_H):
        c = int(round(centro))                         # laterais: abaixo da cava até a barra
        if c - K_LADO <= k < c + K_LADO and r >= r_cava_fim:
            return [(VB[(c, rr)] / FAIXA_REP, (kk - (c - K_LADO)) / (2 * K_LADO)) for kk, rr in cantos]
    if NV_H - K_COSTAS <= k < NV_H + K_COSTAS and r >= NY:   # centro das costas, abaixo da pala
        return [(VB[(NV_H, rr)] / FAIXA_REP, (kk - (NV_H - K_COSTAS)) / (2 * K_COSTAS)) for kk, rr in cantos]
    return None

TECIDO, FAIXA = 0, 1
pino = []
for k in range(NU):
    for r in range(NR):
        if na_cava(k, r):
            continue
        cantos = [(k, r), (k + 1, r), (k + 1, r + 1), (k, r + 1)]
        f = bm.faces.new([vert(chave("b", *c)) for c in cantos])
        fx = faixa_corpo(k, r)
        f.material_index = FAIXA if fx else TECIDO
        uvs = fx or [(UB[c] / TECIDO_L, -VB[c] / TECIDO_A) for c in cantos]
        for l, t in zip(f.loops, uvs):
            l[uv].uv = t

for i in range(NU_H):
    for j in range(NV_H):
        cantos = [(i, j), (i + 1, j), (i + 1, j + 1), (i, j + 1)]
        vs = [vert(chave("h", *c)) for c in cantos]
        if len(set(vs)) < 4:
            continue
        f = bm.faces.new(vs)
        if j < K_ROSTO:                                # borda do rosto
            f.material_index = FAIXA
            uvs = [(UH[(ii, 0)] / FAIXA_REP, jj / K_ROSTO) for ii, jj in cantos]
        else:
            f.material_index = TECIDO
            uvs = [(UH[c] / TECIDO_L, VH[c] / TECIDO_A) for c in cantos]
        for l, t in zip(f.loops, uvs):
            l[uv].uv = t

# canto de cima atrás do capuz: o vértice que sobra com só duas faces some, e as duas viram um quadrado
canto = verts.get(chave("h", NU_H // 2, NV_H))
if canto is not None and len(canto.link_faces) == 2:
    bmesh.ops.dissolve_verts(bm, verts=[canto])

# normais para fora (para longe do eixo do corpo)
bm.normal_update()
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
amostra = [f for f in bm.faces if f.calc_center_median().z < 1.2][:200]
fora = sum(f.normal.dot(Vector((f.calc_center_median().x, f.calc_center_median().y - float(eixo_y(f.calc_center_median().z)), 0)))
           for f in amostra)
if fora < 0:
    bmesh.ops.reverse_faces(bm, faces=bm.faces)

me = bpy.data.meshes.new("Ninsar_Manto")
bm.to_mesh(me)
lados = {}
for f in bm.faces:
    lados[len(f.verts)] = lados.get(len(f.verts), 0) + 1
print("MOLDE faces", len(bm.faces), "por lados", lados, "verts", len(bm.verts))
for f in bm.faces:
    if len(f.verts) != 4:
        print("  face de", len(f.verts), "lados em", tuple(round(x, 3) for x in f.calc_center_median()))
bm.verts.index_update()
indice = {c: v.index for c, v in verts.items() if v.is_valid}
bm.free()
manto = bpy.data.objects.new("Ninsar_Manto", me)
cena.collection.objects.link(manto)
for p in me.polygons:
    p.use_smooth = True

# Materiais
def material(nome, prefixo, rugosidade_tecido):
    m = bpy.data.materials.get(nome) or bpy.data.materials.new(nome)
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    def tex(sufixo, cor):
        n = nt.nodes.new("ShaderNodeTexImage")
        n.image = bpy.data.images.load(f"//../Texturas/Processadas/{prefixo}_{sufixo}.png", check_existing=True)
        n.image.colorspace_settings.name = "sRGB" if cor else "Non-Color"
        return n
    nt.links.new(tex("Cor", True).outputs["Color"], bsdf.inputs["Base Color"])
    ouro = tex("Ouro", False).outputs["Color"]
    met = nt.nodes.new("ShaderNodeMath"); met.operation = "MULTIPLY"; met.inputs[1].default_value = 0.85
    nt.links.new(ouro, met.inputs[0]); nt.links.new(met.outputs[0], bsdf.inputs["Metallic"])
    rug = nt.nodes.new("ShaderNodeMapRange")
    rug.inputs["To Min"].default_value, rug.inputs["To Max"].default_value = rugosidade_tecido, 0.38
    nt.links.new(ouro, rug.inputs["Value"]); nt.links.new(rug.outputs["Result"], bsdf.inputs["Roughness"])
    nm = nt.nodes.new("ShaderNodeNormalMap")
    nt.links.new(tex("Normal", False).outputs["Color"], nm.inputs["Color"])
    nt.links.new(nm.outputs["Normal"], bsdf.inputs["Normal"])
    return m
me.materials.append(material("Manto_Tecido", "Manto_Tecido", 0.85))
me.materials.append(material("Manto_Faixa", "Manto_Faixa", 0.8))

# ---------- UV do tecido: planificação (como um molde de costura) ----------
# Só as faces de tecido; as faixas já têm UV própria. Cada pedaço planificado fica com a escala real
# (uma repetição a cada TECIDO_L x TECIDO_A metros) e com o "para cima" do pano para cima na textura.
for o in bpy.context.view_layer.objects:
    o.select_set(False)
manto.select_set(True)
bpy.context.view_layer.objects.active = manto
bpy.ops.object.mode_set(mode="EDIT")
bm = bmesh.from_edit_mesh(me)
uv = bm.loops.layers.uv.active
capuz_faces = set()
for f in bm.faces:
    f.select = f.material_index == TECIDO
bmesh.update_edit_mesh(me)
# costura entre capuz e corpo: a gola (fileira de cima do corpo)
gola = {indice[("b", k, 0)] for k in range(NU + 1)}
bm.verts.ensure_lookup_table()
# e a costura do meio do capuz (do alto da testa até a nuca), que divide o capuz em duas metades
# e a costura de trás (a fileira dobrada), para as duas metades se separarem de verdade
meio = {indice[chave("h", NU_H // 2, j)] for j in range(NV_H + 1) if chave("h", NU_H // 2, j) in indice}
tras = {indice[chave("h", i, NV_H)] for i in range(NU_H // 2 + 1) if chave("h", i, NV_H) in indice}
# e as costuras do corpo, como numa roupa de verdade: ombros (pela coluna das cavas, até o fim delas) e
# centro das costas na pala; assim cada painel é planificado sozinho e os losangos ficam em pé
costuras_corpo = set()
def costura(k, r):   # aresta vertical (k, r)-(k, r+1); dentro da cava ela não existe
    if ("b", k, r) in indice and ("b", k, r + 1) in indice:
        costuras_corpo.add(frozenset((indice[("b", k, r)], indice[("b", k, r + 1)])))
for c in colunas_lado:
    for r in range(r_cava_fim):
        costura(c, r)
for r in range(NY):
    costura(NV_H, r)
for e in bm.edges:
    ids = {e.verts[0].index, e.verts[1].index}
    e.seam = ids <= gola or ids <= meio or ids <= tras or frozenset(ids) in costuras_corpo
bmesh.update_edit_mesh(me)
bpy.ops.uv.unwrap(method="ANGLE_BASED", margin=0.0)
bm = bmesh.from_edit_mesh(me)
uv = bm.loops.layers.uv.active
tecido = [f for f in bm.faces if f.material_index == TECIDO]
visto, ilhas = set(), []
for f in tecido:
    if f.index in visto:
        continue
    pilha, ilha = [f], []
    visto.add(f.index)
    while pilha:
        a = pilha.pop()
        ilha.append(a)
        for e in a.edges:
            if e.seam:
                continue
            for b in e.link_faces:
                if b.index not in visto and b.material_index == TECIDO:
                    visto.add(b.index)
                    pilha.append(b)
    ilhas.append(ilha)
for ilha in ilhas:
    area3d = sum(f.calc_area() for f in ilha)
    areauv = 0.0
    cima = Vector((0.0, 0.0))
    for f in ilha:
        ls = f.loops
        for i in range(1, len(ls) - 1):
            a, b, c = ls[0][uv].uv, ls[i][uv].uv, ls[i + 1][uv].uv
            areauv += abs((b - a).cross(c - a)) / 2
        for l in ls:
            d = l.link_loop_next.vert.co - l.vert.co
            cima += (l.link_loop_next[uv].uv - l[uv].uv) * d.z
    if areauv <= 0 or cima.length == 0:
        continue
    escala = math.sqrt(area3d / areauv)
    ang = math.atan2(cima.x, cima.y)       # gira para "cima" do pano virar +V
    cs, sn = math.cos(ang), math.sin(ang)
    for f in ilha:
        for l in f.loops:
            u0, v0 = l[uv].uv
            u1, v1 = u0 * cs - v0 * sn, u0 * sn + v0 * cs
            l[uv].uv = (u1 * escala / TECIDO_L, v1 * escala / TECIDO_A)
print("UV do tecido:", len(ilhas), "pedaços planificados")
bmesh.update_edit_mesh(me)
bpy.ops.object.mode_set(mode="OBJECT")

# ---------- Simulação ----------
grupo = manto.vertex_groups.new(name="Pino")
pinos = [indice[c] for c in indice if c[0] == "b" and (c[2] <= NY or
         ((c[1] <= K_FRENTE or c[1] >= NU - K_FRENTE) and pos[c].z > BUSTO))]
pinos += [indice[c] for c in indice if c[0] == "h" and (pos[c] - C).normalized().z > CALOTA]
grupo.add(pinos, 1.0, "REPLACE")

colisores = []
def colisor(o, espessura):
    m = o.modifiers.new("Colisao", "COLLISION")
    o.collision.thickness_outer = espessura
    o.collision.cloth_friction = 5.0
    colisores.append((o, m))
colisor(corpo, 0.006)
# A saia de verdade tem franjas e camadas onde o pano engancha. O colisor é um tubo liso com o
# contorno dela ("forma de costureira"), que nunca afina para baixo.
DA_T, DZ_T = 6, 0.03
psi_s = np.degrees(np.arctan2(sv[:, 0], sv[:, 1] - eixo_y(sv[:, 2]))) % 360
r_s = np.hypot(sv[:, 0], sv[:, 1] - eixo_y(sv[:, 2]))
z_alto, z_baixo = sv[:, 2].max(), sv[:, 2].min()
nz = int((z_alto - z_baixo) / DZ_T) + 1
tubo = np.zeros((360 // DA_T, nz))
np.maximum.at(tubo, ((psi_s // DA_T).astype(int) % (360 // DA_T), ((z_alto - sv[:, 2]) / DZ_T).astype(int).clip(0, nz - 1)), r_s)
tubo = np.maximum.accumulate(tubo, axis=1)                       # de cima para baixo, nunca afina
tubo = (np.roll(tubo, 1, 0) + tubo + np.roll(tubo, -1, 0)) / 3 + 0.01
tm = bpy.data.meshes.new("Saia_Colisor")
tv = [ponto_cilindro(a * DA_T, z_alto - j * DZ_T, tubo[a, j])[:] for j in range(nz) for a in range(360 // DA_T)]
na = 360 // DA_T
tf = [(j * na + a, j * na + (a + 1) % na, (j + 1) * na + (a + 1) % na, (j + 1) * na + a)
      for j in range(nz - 1) for a in range(na)]
tm.from_pydata(tv, [], tf)
proxy = bpy.data.objects.new("Saia_Colisor", tm)
cena.collection.objects.link(proxy)
proxy.hide_render = True
colisor(proxy, 0.006)

pano = manto.modifiers.new("Tecido", "CLOTH")
s = pano.settings
s.quality = 10
s.mass = 0.5
s.air_damping = 2.0
s.tension_stiffness = s.compression_stiffness = 80
s.shear_stiffness = 10
s.bending_stiffness = 3.0
s.vertex_group_mass = "Pino"
cs = pano.collision_settings
cs.use_collision = True
cs.distance_min = 0.006
cs.collision_quality = 4
cs.use_self_collision = True
cs.self_distance_min = 0.004
pano.point_cache.frame_start, pano.point_cache.frame_end = 1, QUADROS
cena.frame_start, cena.frame_end = 1, QUADROS
for q in range(1, QUADROS + 1):
    cena.frame_set(q)
    if q % 10 == 0:
        print("  simulação quadro", q)

for o in bpy.context.view_layer.objects:
    o.select_set(False)
manto.select_set(True)
bpy.context.view_layer.objects.active = manto
bpy.ops.object.modifier_apply(modifier="Tecido")
cena.frame_set(1)
for o, m in colisores:
    o.modifiers.remove(m)
bpy.data.objects.remove(proxy)

zs = [v.co.z for v in me.vertices]
print("MANTO", len(me.polygons), "faces; barra a", round(min(zs), 3), "m; topo a", round(max(zs), 3), "m")
bpy.ops.wm.save_mainfile(compress=True)
print("SALVO")
