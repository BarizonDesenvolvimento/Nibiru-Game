# Etapa 2 (manto), versão capa-poncho: manto novo e limpo, feito como molde de roupa e vestido com
# simulação de tecido, seguindo as referências (folhas do manto e a Ninsar vestida em movimento):
# - frente bem aberta: as bordas bordadas descem na linha da frente dos braços, que ficam livres;
# - sem cavas: o pano passa por cima dos ombros e cai solto, longo, arrastando no chão atrás;
# - uma aba solta sobre os ombros (capinha) cobre os braços até perto do cotovelo, com faixa e franja;
# - capuz largo, com folga em volta do rosto e a borda da abertura no alto da cabeça (jogado para trás);
# - tecido mole e pesado, simulado com os braços baixados (a pose fica no esqueleto, sem aplicar).
# O manto do Meshy vira só referência (Ref_Manto_Meshy, escondido).
#
# Malha: quadrados, uma face só. Corpo do manto em colunas (em volta do corpo) e fileiras (do pescoço à
# barra); capuz em grade em volta da cabeça, com a fileira de trás dobrada sobre ela mesma; a primeira
# coluna do capuz é a própria gola. Franjas: tiras recortadas por transparência na barra da aba.
# Materiais: Manto_Tecido (tecido repetível, UV planificada como molde), Manto_Faixa (bordado: U ao
# longo da faixa, V de uma linha dourada à outra) e Manto_Franja (franja com transparência).
# Uso: blender -b Ninsar_Final.blend -P Scripts/etapa2_manto_poncho.py
import bpy, bmesh, math, os
import numpy as np
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

# --- Ajustes ---
NV_H = 36            # quadrados do capuz da frente até a nuca (de cada lado); o corpo tem 2*NV_H colunas
NU_H = 64            # quadrados do capuz em volta do rosto (par)
NY = 5               # fileiras da pala (do pescoço até a linha dos ombros)
NM = 56              # fileiras do corpo, da linha dos ombros até a barra
FRENTE = 104.0       # ângulo das bordas da frente (0 = costas, 180 = frente): na linha da frente dos braços
GOLA_FRENTE = 160.0  # ângulo da gola nas bordas da frente
BARRA_FRENTE, BARRA_COSTAS = 0.03, -0.30   # altura da barra no molde: na frente, e atrás (sobra = cauda no chão)
FOLGA, EVASE = 0.04, 0.10                  # folga inicial sobre corpo/saia e quanto abre por metro de queda
BRACOS = 72.0        # graus que os braços descem da pose T para a simulação
BRACOS_FRENTE = 14.0 # e quanto vão para a frente (antebraços livres, na frente das bordas do manto)
CAPUZ_FOLGA, CAPUZ_FOLGA_ROSTO = 0.05, 0.08
CAPUZ_TOPO = 74.0    # altura (graus, visto do centro da cabeça) da borda da abertura no alto: capuz para trás
CALOTA = 0.9         # pontos do capuz acima deste ângulo ficam presos (ele não escorrega de vez)
ABA_ATE = 1.08       # altura da barra da aba sobre os ombros (perto do cotovelo, com os braços baixados)
ABA_FOLGA = 0.03
NA, NAF = 10, 2      # fileiras da aba e da franja
FRANJA = 0.07        # comprimento da franja (m)
TECIDO_L = 0.225                              # largura de uma repetição do tecido (m): treliça menor
TECIDO_A = TECIDO_L * 1140 / 1045
FAIXA_LARG = 0.12                             # largura de referência das faixas largas (m)
FAIXA_REP = FAIXA_LARG * 1085 / 415           # um ciclo do bordado ao longo da faixa (m)
K_FRENTE, K_COSTAS, K_ROSTO = 5, 3, 10       # larguras das faixas da frente/costas/rosto (em quadrados)
LARG_BARRA, LARG_ABA = 0.13, 0.08            # largura das faixas da barra e da aba (m)
FRANJA_REP = 0.12                             # uma repetição da textura de franja ao longo da barra (m)
QUADROS = 150
# ----------------

cena = bpy.context.scene
arm = bpy.data.objects["Armature"]
corpo = bpy.data.objects["Ninsar_Corpo"]
saia = bpy.data.objects["Ninsar_Saia"]
lanca = bpy.data.objects["Ninsar_Lanca"]
if "Ref_Manto_Meshy" not in bpy.data.objects:
    bpy.data.objects["Ninsar_Manto"].name = "Ref_Manto_Meshy"
elif "Ninsar_Manto" in bpy.data.objects:
    bpy.data.objects.remove(bpy.data.objects["Ninsar_Manto"])
velho = bpy.data.objects["Ref_Manto_Meshy"]
velho.hide_render = velho.hide_viewport = True

# ---------- Braços baixados (só pose; a lança acompanha a mão) ----------
def girar(nome, eixo, graus):
    pb = arm.pose.bones[nome]
    bpy.context.view_layer.update()
    cabeca = arm.matrix_world @ pb.head
    R = Matrix.Translation(cabeca) @ Matrix.Rotation(math.radians(graus), 4, eixo) @ Matrix.Translation(-cabeca)
    pb.matrix = arm.matrix_world.inverted() @ R @ arm.matrix_world @ pb.matrix
    bpy.context.view_layer.update()
mao = arm.pose.bones["CC_Base_R_Hand"]
antes = arm.matrix_world @ mao.matrix
girar("CC_Base_R_Upperarm", "Y", -BRACOS)
girar("CC_Base_L_Upperarm", "Y", BRACOS)
girar("CC_Base_R_Upperarm", "X", -BRACOS_FRENTE)
girar("CC_Base_L_Upperarm", "X", -BRACOS_FRENTE)
lanca.matrix_world = (arm.matrix_world @ mao.matrix) @ antes.inverted() @ lanca.matrix_world
lanca.hide_render = True

dg = bpy.context.evaluated_depsgraph_get()
def malha(o):
    me = o.evaluated_get(dg).to_mesh()
    return np.array([(o.matrix_world @ v.co)[:] for v in me.vertices]), [tuple(p.vertices) for p in me.polygons]
cv, cf = malha(corpo)
bvh_c = BVHTree.FromPolygons([Vector(v) for v in cv], cf)
sv, _ = malha(saia)

tronco = cv[np.abs(cv[:, 0]) < 0.18]
alturas = np.arange(0, 2.0, 0.05)
centros = np.array([tronco[np.abs(tronco[:, 2] - z) < 0.025, 1].mean()
                    if (np.abs(tronco[:, 2] - z) < 0.025).any() else 0.0 for z in alturas])
def eixo_y(z):
    return np.interp(z, alturas, centros)
cab = cv[(cv[:, 2] > 1.70) & (np.abs(cv[:, 0]) < 0.12)]
C = Vector(((cab[:, 0].min() + cab[:, 0].max()) / 2, (cab[:, 1].min() + cab[:, 1].max()) / 2, 1.78))

# Envelope (raio máximo por ângulo e altura) do corpo, já com os braços baixados, + saia
cotovelo_z = min((arm.matrix_world @ arm.pose.bones[n].head).z for n in ("CC_Base_R_Forearm", "CC_Base_L_Forearm"))
antebraco = (np.abs(cv[:, 0]) > 0.16) & (cv[:, 2] < cotovelo_z + 0.03) & (cv[:, 2] > 0.6)
pts = np.vstack([cv[~antebraco], sv])
DZ, DA = 0.03, 4
psi_p = np.degrees(np.arctan2(pts[:, 0], pts[:, 1] - eixo_y(pts[:, 2]))) % 360
r_p = np.hypot(pts[:, 0], pts[:, 1] - eixo_y(pts[:, 2]))
env = np.zeros((360 // DA, int(2.0 / DZ) + 1))
np.maximum.at(env, ((psi_p // DA).astype(int) % (360 // DA), (pts[:, 2] / DZ).astype(int).clip(0, env.shape[1] - 1)), r_p)
env = np.maximum(env, np.maximum(np.roll(env, 1, 0), np.roll(env, -1, 0)))
def envelope(psi, z):
    return env[int((psi % 360) // DA), int(np.clip(z / DZ, 0, env.shape[1] - 1))]

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

def suave(t):
    t = min(max(t, 0.0), 1.0)
    return t * t * (3 - 2 * t)

# ---------- Capuz ----------
def esf(phi, theta):
    p, t = math.radians(phi), math.radians(theta)
    return Vector((math.sin(p) * math.cos(t), math.cos(p) * math.cos(t), math.sin(t)))
def bezier(a, b, c, s):
    return tuple((1 - s) ** 2 * a[i] + 2 * (1 - s) * s * b[i] + s * s * c[i] for i in range(2))
def espelho(v):
    return Vector((-v.x, v.y, v.z))
TETA_GOLA = -58.0
def F(u):   # borda da abertura: do pescoço (frente, esquerda) passando ao lado do rosto até o alto da cabeça
    s = 2 * u if u <= 0.5 else 2 * (1 - u)
    d = esf(*bezier((GOLA_FRENTE, TETA_GOLA), (95, 10), (180, CAPUZ_TOPO), s))
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
    return (tipo, a, b)

# ---------- Corpo do manto ----------
NU = 2 * NV_H
def psi_coluna(k):
    return FRENTE * (1 - k / NV_H) if k <= NV_H else -FRENTE * (k / NV_H - 1)
def ponto_cilindro(psi, z, r):
    a = math.radians(psi)
    return Vector((r * math.sin(a), float(eixo_y(max(z, 0.0))) + r * math.cos(a), z))
def barra(psi):   # longo na frente, mais longo atrás (cauda)
    return BARRA_COSTAS + (BARRA_FRENTE - BARRA_COSTAS) * suave(abs(psi) / FRENTE)
Z_OMBRO = 1.46
for k in range(NU + 1):
    pos[("b", k, 0)] = pos[("h", 0, k)] if k <= NV_H else pos[("h", NU_H, NU - k)]
    psi = psi_coluna(k)
    S = empurrar_ponto(ponto_cilindro(psi, Z_OMBRO, envelope(psi, Z_OMBRO) + 0.02), 0.02)
    T = pos[("b", k, 0)]
    for r in range(1, NY + 1):
        pos[("b", k, r)] = empurrar_ponto(T.lerp(S, r / NY) if r < NY else S, 0.015)
    col = env[int((psi % 360) // DA)]
    h = barra(psi)
    for m in range(1, NM + 1):
        z = Z_OMBRO + (h - Z_OMBRO) * m / NM
        zc = max(z, 0.0)
        maxr = col[int(zc / DZ): int(Z_OMBRO / DZ) + 1].max()
        p = ponto_cilindro(psi, z, maxr + FOLGA + EVASE * (Z_OMBRO - zc))
        pos[("b", k, NY + m)] = empurrar_ponto(p, 0.015) if z > 0.05 else p
NR = NY + NM

# ---------- Aba sobre os ombros (peça solta, presa pela borda de cima) ----------
# A aba nasce no meio da pala e segue o caminho da coluna do manto (ombro abaixo) até ABA_ATE,
# um pouco por fora dele: assim ela acompanha a inclinação do ombro em vez de formar uma prateleira.
R0 = 2
for k in range(NU + 1):
    caminho = [pos[("b", k, r)] for r in range(R0, NR + 1)]
    fim = next((i for i, p in enumerate(caminho) if p.z <= ABA_ATE), len(caminho) - 1)
    caminho = caminho[:fim + 1]
    comp = [0.0]
    for a, b in zip(caminho, caminho[1:]):
        comp.append(comp[-1] + (b - a).length)
    for m in range(NA + 1):
        alvo = comp[-1] * m / NA
        i = max(j for j in range(len(comp)) if comp[j] <= alvo + 1e-9)
        i = min(i, len(caminho) - 2)
        t = (alvo - comp[i]) / max(comp[i + 1] - comp[i], 1e-6)
        p = caminho[i].lerp(caminho[i + 1], t)
        fora = Vector((p.x, p.y - float(eixo_y(p.z)), 0.0)).normalized()
        pos[("a", k, m)] = p + (fora + Vector((0, 0, 0.6))).normalized() * ABA_FOLGA * (0.6 + m / NA)
    for m in range(1, NAF + 1):   # franja da aba
        pos[("fa", k, m)] = pos[("a", k, NA)] + Vector((0, 0, -FRANJA * m / NAF))

# ---------- Malha, materiais e UVs ----------
bm = bmesh.new()
uv = bm.loops.layers.uv.new("UVMap")
verts = {}
def vert(c):
    if c not in verts:
        verts[c] = bm.verts.new(pos[c])
    return verts[c]

def comprimentos(tipo, ncol, nlin, centro):
    """Comprimento no molde ao longo das fileiras (a partir da coluna 'centro') e das colunas."""
    U, V = {}, {}
    for r in range(nlin + 1):
        U[(centro, r)] = 0.0
        for passo in (1, -1):
            acc, k = 0.0, centro
            while 0 <= k + passo <= ncol:
                acc += (pos[(tipo, k + passo, r)] - pos[(tipo, k, r)]).length
                k += passo
                U[(k, r)] = acc * passo
    for k in range(ncol + 1):
        acc = 0.0
        V[(k, 0)] = 0.0
        for r in range(1, nlin + 1):
            acc += (pos[(tipo, k, r)] - pos[(tipo, k, r - 1)]).length
            V[(k, r)] = acc
    return U, V
UB, VB = comprimentos("b", NU, NR, NV_H)
UA, VA = comprimentos("a", NU, NA, NV_H)
UH, VH = comprimentos("h", NU_H, NV_H, NU_H // 2)

TECIDO, FAIXA, FRANJA_M = 0, 1, 2
def face(cantos_chave, mat, uvs):
    vs = [vert(chave(*c)) for c in cantos_chave]
    if len(set(vs)) < len(vs):
        return None
    f = bm.faces.new(vs)
    f.material_index = mat
    for l, t in zip(f.loops, uvs):
        l[uv].uv = t
    return f

for k in range(NU):
    for r in range(NR):
        cantos = [(k, r), (k + 1, r), (k + 1, r + 1), (k, r + 1)]
        ck = [("b",) + c for c in cantos]
        if k < K_FRENTE or k >= NU - K_FRENTE:                       # bordas da frente
            borda = 0 if k < K_FRENTE else NU
            face(ck, FAIXA, [(VB[(borda, rr)] / FAIXA_REP, abs(kk - borda) / K_FRENTE) for kk, rr in cantos])
        elif VB[(k, NR)] - VB[(k, r + 1)] < LARG_BARRA:               # barra
            face(ck, FAIXA, [(UB[(kk, NR)] / FAIXA_REP, min((VB[(kk, NR)] - VB[(kk, rr)]) / LARG_BARRA, 1.0))
                             for kk, rr in cantos])
        elif NV_H - K_COSTAS <= k < NV_H + K_COSTAS and r >= NY:       # faixa do centro das costas
            face(ck, FAIXA, [(VB[(NV_H, rr)] / FAIXA_REP, (kk - (NV_H - K_COSTAS)) / (2 * K_COSTAS)) for kk, rr in cantos])
        else:
            face(ck, TECIDO, [(0.0, 0.0)] * 4)                        # planificada depois

for k in range(NU):
    for m in range(NA):                                                 # aba
        cantos = [(k, m), (k + 1, m), (k + 1, m + 1), (k, m + 1)]
        ck = [("a",) + c for c in cantos]
        if VA[(k, NA)] - VA[(k, m + 1)] < LARG_ABA:
            face(ck, FAIXA, [(UA[(kk, NA)] / FAIXA_REP, min((VA[(kk, NA)] - VA[(kk, mm)]) / LARG_ABA, 1.0))
                             for kk, mm in cantos])
        else:
            face(ck, TECIDO, [(0.0, 0.0)] * 4)
    for m in range(NAF):                                                # franja da aba
        c0 = ("a", k, NA) if m == 0 else ("fa", k, m)
        c1 = ("a", k + 1, NA) if m == 0 else ("fa", k + 1, m)
        face([c0, c1, ("fa", k + 1, m + 1), ("fa", k, m + 1)], FRANJA_M,
             [(UA[(k, NA)] / FRANJA_REP, 1 - m / NAF), (UA[(k + 1, NA)] / FRANJA_REP, 1 - m / NAF),
              (UA[(k + 1, NA)] / FRANJA_REP, 1 - (m + 1) / NAF), (UA[(k, NA)] / FRANJA_REP, 1 - (m + 1) / NAF)])

for i in range(NU_H):
    for j in range(NV_H):
        cantos = [(i, j), (i + 1, j), (i + 1, j + 1), (i, j + 1)]
        ck = [("h",) + c for c in cantos]
        if j < K_ROSTO:                                                  # borda da abertura do capuz
            face(ck, FAIXA, [(UH[(ii, 0)] / FAIXA_REP, jj / K_ROSTO) for ii, jj in cantos])
        else:
            face(ck, TECIDO, [(0.0, 0.0)] * 4)

canto = verts.get(chave("h", NU_H // 2, NV_H))
if canto is not None and len(canto.link_faces) == 2:
    bmesh.ops.dissolve_verts(bm, verts=[canto])

bm.normal_update()
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
amostra = [f for f in bm.faces if 0.3 < f.calc_center_median().z < 1.2][:300]
fora = sum(f.normal.dot(Vector((f.calc_center_median().x, f.calc_center_median().y - float(eixo_y(f.calc_center_median().z)), 0)))
           for f in amostra)
if fora < 0:
    bmesh.ops.reverse_faces(bm, faces=bm.faces)
bm.verts.index_update()
indice = {c: v.index for c, v in verts.items() if v.is_valid}
lados = {}
for f in bm.faces:
    lados[len(f.verts)] = lados.get(len(f.verts), 0) + 1
print("MOLDE faces", len(bm.faces), "por lados", lados, "verts", len(bm.verts))
me = bpy.data.meshes.new("Ninsar_Manto")
bm.to_mesh(me)
bm.free()
manto = bpy.data.objects.new("Ninsar_Manto", me)
cena.collection.objects.link(manto)
for p in me.polygons:
    p.use_smooth = True

# Textura provisória da franja (fios dourados com transparência), se ainda não existir
PROC = os.path.normpath(os.path.join(os.path.dirname(bpy.data.filepath), "..", "Texturas", "Processadas"))
arq_franja = os.path.join(PROC, "Manto_Franja_Cor.png")
if not os.path.exists(arq_franja):
    W, H = 256, 128
    rng = np.random.default_rng(3)
    px = np.zeros((H, W, 4), dtype=np.float32)
    ouro = np.array([0.62, 0.45, 0.16])
    px[: H // 5, :, :3] = ouro * 0.8                      # faixa dos nós, em cima
    px[: H // 5, :, 3] = 1.0
    for x0 in range(0, W, 16):                            # fios (16 por repetição, ~7 mm entre eles)
        fim = int(H * rng.uniform(0.8, 1.0))
        tom = ouro * rng.uniform(0.75, 1.15)
        px[H // 5:fim, x0 + 4:x0 + 12, :3] = tom
        px[H // 5:fim, x0 + 4:x0 + 12, 3] = 1.0
    im = bpy.data.images.new("Manto_Franja_Cor", W, H, alpha=True)
    im.pixels.foreach_set(px[::-1].ravel())
    im.filepath_raw = arq_franja
    im.file_format = "PNG"
    im.save()
    bpy.data.images.remove(im)

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
    cor = tex("Cor", True)
    nt.links.new(cor.outputs["Color"], bsdf.inputs["Base Color"])
    if prefixo == "Manto_Franja":
        nt.links.new(cor.outputs["Alpha"], bsdf.inputs["Alpha"])
        bsdf.inputs["Metallic"].default_value, bsdf.inputs["Roughness"].default_value = 0.7, 0.45
        return m
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
me.materials.append(material("Manto_Franja", "Manto_Franja", 0.5))

# ---------- UV do tecido: planificação por painéis (como um molde de costura) ----------
for o in bpy.context.view_layer.objects:
    o.select_set(False)
manto.select_set(True)
bpy.context.view_layer.objects.active = manto
bpy.ops.object.mode_set(mode="EDIT")
bm = bmesh.from_edit_mesh(me)
for f in bm.faces:
    f.select = f.material_index == TECIDO
k_lado = [int(round(NV_H - (FRENTE - a) / FRENTE * NV_H)) for a in (90.0,)]
k_lado = [k_lado[0], NU - k_lado[0]]
costuras = set()
def costura(a, b):
    if a in indice and b in indice:
        costuras.add(frozenset((indice[a], indice[b])))
for k in range(NU + 1):                                   # gola (capuz | corpo)
    costura(("b", k, 0), ("b", k + 1, 0))
for j in range(NV_H):                                     # meio do capuz
    costura(chave("h", NU_H // 2, j), chave("h", NU_H // 2, j + 1))
for i in range(NU_H // 2):                                # costura de trás do capuz
    costura(chave("h", i, NV_H), chave("h", i + 1, NV_H))
for k in k_lado:                                          # laterais (do ombro à barra) e da aba
    for r in range(NR):
        costura(("b", k, r), ("b", k, r + 1))
    for m in range(NA):
        costura(("a", k, m), ("a", k, m + 1))
for r in range(NY):                                       # centro das costas na pala
    costura(("b", NV_H, r), ("b", NV_H, r + 1))
for m in range(NA):
    costura(("a", NV_H, m), ("a", NV_H, m + 1))
bm.edges.ensure_lookup_table()
for e in bm.edges:
    e.seam = frozenset((e.verts[0].index, e.verts[1].index)) in costuras
bmesh.update_edit_mesh(me)
bpy.ops.uv.unwrap(method="ANGLE_BASED", margin=0.0)
bm = bmesh.from_edit_mesh(me)
uvl = bm.loops.layers.uv.active
visto, ilhas = set(), []
for f in bm.faces:
    if f.material_index != TECIDO or f.index in visto:
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
    areauv, cima = 0.0, Vector((0.0, 0.0))
    for f in ilha:
        ls = f.loops
        for i in range(1, len(ls) - 1):
            a, b, c = ls[0][uvl].uv, ls[i][uvl].uv, ls[i + 1][uvl].uv
            areauv += abs((b - a).cross(c - a)) / 2
        for l in ls:
            cima += (l.link_loop_next[uvl].uv - l[uvl].uv) * (l.link_loop_next.vert.co - l.vert.co).z
    if areauv <= 0 or cima.length == 0:
        continue
    escala = math.sqrt(area3d / areauv)
    ang = math.atan2(cima.x, cima.y)
    cs, sn = math.cos(ang), math.sin(ang)
    for f in ilha:
        for l in f.loops:
            u0, v0 = l[uvl].uv
            l[uvl].uv = ((u0 * cs - v0 * sn) * escala / TECIDO_L, (u0 * sn + v0 * cs) * escala / TECIDO_A)
print("UV do tecido:", len(ilhas), "painéis planificados")
bmesh.update_edit_mesh(me)
bpy.ops.object.mode_set(mode="OBJECT")

# ---------- Simulação ----------
grupo = manto.vertex_groups.new(name="Pino")
pinos = [indice[c] for c in indice if c[0] == "b" and c[2] <= NY]
pinos += [indice[c] for c in indice if c[0] == "a" and c[2] == 0]
pinos += [indice[c] for c in indice if c[0] == "h" and (pos[c] - C).normalized().z > CALOTA]
grupo.add(pinos, 1.0, "REPLACE")

colisores = []
def colisor(o, espessura):
    m = o.modifiers.new("Colisao", "COLLISION")
    o.collision.thickness_outer = espessura
    o.collision.cloth_friction = 5.0
    colisores.append((o, m))
colisor(corpo, 0.006)
# Saia: tubo liso com o contorno dela ("forma de costureira"), que nunca afina para baixo
DA_T, DZ_T = 6, 0.03
psi_s = np.degrees(np.arctan2(sv[:, 0], sv[:, 1] - eixo_y(sv[:, 2]))) % 360
r_s = np.hypot(sv[:, 0], sv[:, 1] - eixo_y(sv[:, 2]))
z_alto, z_baixo = sv[:, 2].max(), max(sv[:, 2].min(), 0.0)
nz = int((z_alto - z_baixo) / DZ_T) + 1
tubo = np.zeros((360 // DA_T, nz))
np.maximum.at(tubo, ((psi_s // DA_T).astype(int) % (360 // DA_T), ((z_alto - np.maximum(sv[:, 2], 0)) / DZ_T).astype(int).clip(0, nz - 1)), r_s)
tubo = np.maximum.accumulate(tubo, axis=1)
tubo = (np.roll(tubo, 1, 0) + tubo + np.roll(tubo, -1, 0)) / 3 + 0.01
na = 360 // DA_T
tm = bpy.data.meshes.new("Saia_Colisor")
tm.from_pydata([ponto_cilindro(a * DA_T, z_alto - j * DZ_T, tubo[a, j])[:] for j in range(nz) for a in range(na)], [],
               [(j * na + a, j * na + (a + 1) % na, (j + 1) * na + (a + 1) % na, (j + 1) * na + a)
                for j in range(nz - 1) for a in range(na)])
proxy = bpy.data.objects.new("Saia_Colisor", tm)
cena.collection.objects.link(proxy)
colisor(proxy, 0.006)
chao_m = bpy.data.meshes.new("Chao_Colisor")
chao_m.from_pydata([(-3, -3, 0), (3, -3, 0), (3, 3, 0), (-3, 3, 0)], [], [(0, 1, 2, 3)])
chao = bpy.data.objects.new("Chao_Colisor", chao_m)
cena.collection.objects.link(chao)
colisor(chao, 0.005)

pano = manto.modifiers.new("Tecido", "CLOTH")
s = pano.settings
s.quality = 10
s.mass = 0.8                       # pesado
s.air_damping = 3.0
s.tension_stiffness, s.compression_stiffness = 60, 30
s.shear_stiffness = 10
s.bending_stiffness = 0.5          # mole: dobras verticais
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
    if q % 30 == 0:
        print("  simulação quadro", q)
bpy.ops.object.modifier_apply(modifier="Tecido")
cena.frame_set(1)
for o, m in colisores:
    o.modifiers.remove(m)
for o in (proxy, chao):
    bpy.data.objects.remove(o)
zs = [v.co.z for v in me.vertices]
print("MANTO", len(me.polygons), "faces; mais baixo", round(min(zs), 3), "m; topo", round(max(zs), 3), "m")
bpy.ops.wm.save_mainfile(compress=True)
print("SALVO")
