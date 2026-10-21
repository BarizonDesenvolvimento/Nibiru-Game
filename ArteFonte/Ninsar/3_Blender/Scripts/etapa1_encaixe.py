# Etapa 1b: veste as peças na Ninsar (em Ninsar_Final.blend, que já passou pela etapa1_escala.py).
# 1. Ajuste rígido de cada peça (valores achados com otimizar_encaixe.py e conferidos nos renders).
# 2. "Empurrão" suave, só do quadril para cima: onde o corpo (ou a face de fora da saia, no caso do manto) atravessa o pano, um campo de
#    deslocamento suave empurra o pano para fora. As duas faces do pano andam juntas, então o relevo
#    dos bordados não se deforma.
# 3. Lança na mão direita: o cabo atravessa o punho (por baixo da palma), com a ponta para a frente.
#    Na pose de repouso (braços abertos) ela fica deitada; com o braço baixado e o antebraço para a
#    frente, fica em pé.
# Uso: blender -b Ninsar_Final.blend -P Scripts/etapa1_encaixe.py
import bpy, math
import numpy as np
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree

# --- Ajustes (mude aqui e rode de novo a partir da etapa1_escala.py) ---
CINTURA, BARRA = 1.17, 0.02        # altura da borda de cima e da barra da saia (m)
CINTO_BAIXO = 1.135                # borda de baixo do cinto de couro (parte do corpo), nas laterais
MEDALHAO = dict(meia_largura=0.05, baixo=1.08)   # o medalhão desce mais no meio da frente
CINTO_TRANSICAO = 0.02             # faixa logo abaixo do cinto onde a saia volta aos poucos para fora
# Capuz: aproxima do crânio, mirando esta folga; perto do rosto a folga cresce até CAPUZ_FOLGA_ROSTO, para a
# abertura continuar emoldurando a face. Fecha só CAPUZ_FORCA do vão que existe hoje em cada ponto (1 = colado
# na folga), para o capuz manter volume. Age do pescoço para cima, com transição de CAPUZ_INICIO a CAPUZ_INTEIRO.
CAPUZ_FOLGA, CAPUZ_FOLGA_ROSTO, CAPUZ_FORCA = 0.012, 0.04, 0.85
CAPUZ_INICIO, CAPUZ_INTEIRO = 1.56, 1.74
MANTO = dict(pivo=(0.0, 0.05, 1.55), rx=1.0, dy=0.095, dz=0.03, s=1.03)
SAIA = dict(rx=2.5, dy=0.03, s=1.08)
FOLGA_MANTO, FOLGA_SAIA = 0.012, 0.008   # distância mínima entre pano e o que está por baixo (m)
# Nas pernas o pano fica com a forma original do Meshy (só o ajuste rígido): a física de tecido
# trata as colisões ali. Sem empurrão abaixo de SEM_EMPURRAO, empurrão inteiro acima de COM_EMPURRAO.
SEM_EMPURRAO, COM_EMPURRAO = 0.95, 1.12
LANCA_PEGADA = 0.50                # altura (no modelo da lança em pé) do ponto onde a mão segura
# -----------------------------------------------------------------------

arm = bpy.data.objects["Armature"]
corpo = bpy.data.objects["Ninsar_Corpo"]
manto = bpy.data.objects["Ninsar_Manto"]
saia = bpy.data.objects["Ninsar_Saia"]
lanca = bpy.data.objects["Ninsar_Lanca"]

def coords(o):
    a = np.empty(len(o.data.vertices) * 3)
    o.data.vertices.foreach_get("co", a)
    a = a.reshape(-1, 3)
    m = np.array(o.matrix_world)
    return a @ m[:3, :3].T + m[:3, 3]

def gravar(o, mundo):
    m = np.array(o.matrix_world.inverted())
    o.data.vertices.foreach_set("co", (mundo @ m[:3, :3].T + m[:3, 3]).ravel())
    o.data.update()

def transformar(o, M):
    gravar(o, coords(o) @ np.array(M)[:3, :3].T + np.array(M)[:3, 3])

def bvh_mundo(o):
    dg = bpy.context.evaluated_depsgraph_get()
    me = o.evaluated_get(dg).to_mesh()
    vs = [o.matrix_world @ v.co for v in me.vertices]
    return BVHTree.FromPolygons(vs, [tuple(p.vertices) for p in me.polygons])

# ---------- 1. Ajuste rígido ----------
v = coords(saia)
zmin, zmax = v[:, 2].min(), v[:, 2].max()
pivo_saia = Vector((0.0, -0.04, zmax))
sz = (CINTURA - BARRA) / (zmax - zmin)
M_saia = (Matrix.Translation((0, SAIA["dy"], CINTURA - zmax)) @ Matrix.Translation(pivo_saia)
          @ Matrix.Rotation(math.radians(SAIA["rx"]), 4, "X")
          @ Matrix.Diagonal((SAIA["s"], SAIA["s"], sz, 1)) @ Matrix.Translation(-pivo_saia))
transformar(saia, M_saia)

pivo = Vector(MANTO["pivo"])
M_manto = (Matrix.Translation((0, MANTO["dy"], MANTO["dz"])) @ Matrix.Translation(pivo)
           @ Matrix.Rotation(math.radians(MANTO["rx"]), 4, "X")
           @ Matrix.Scale(MANTO["s"], 4) @ Matrix.Translation(-pivo))
transformar(manto, M_manto)

# ---------- 2. Empurrão suave ----------
def campo_suave(pos, desl, h=0.02, raio=3):
    """Espalha os deslocamentos num grade 3D, borra (gaussiana) e lê de volta em cada vértice."""
    lo = pos.min(0) - (raio + 2) * h
    idx = np.floor((pos - lo) / h).astype(int)
    forma = tuple(idx.max(0) + raio + 3)
    soma = np.zeros(forma + (3,))
    peso = np.zeros(forma)
    np.add.at(soma, tuple(idx.T), desl)
    np.add.at(peso, tuple(idx.T), 1.0)
    k = np.exp(-0.5 * (np.arange(-raio, raio + 1) / (raio / 2)) ** 2)
    k /= k.sum()
    for eixo in range(3):
        soma = sum(w * np.roll(soma, s, axis=eixo) for s, w in zip(range(-raio, raio + 1), k))
        peso = sum(w * np.roll(peso, s, axis=eixo) for s, w in zip(range(-raio, raio + 1), k))
    campo = soma / np.maximum(peso, 1e-6)[..., None]
    # leitura trilinear
    f = (pos - lo) / h - 0.5
    i0 = np.floor(f).astype(int)
    t = f - i0
    out = np.zeros_like(pos)
    for dx in (0, 1):
        for dy in (0, 1):
            for dz in (0, 1):
                w = (np.where(dx, t[:, 0], 1 - t[:, 0]) * np.where(dy, t[:, 1], 1 - t[:, 1])
                     * np.where(dz, t[:, 2], 1 - t[:, 2]))
                out += w[:, None] * campo[i0[:, 0] + dx, i0[:, 1] + dy, i0[:, 2] + dz]
    return out

def faces_de_fora(o, eixo_y, minimo=0.2):
    """BVH só com as faces da peça viradas para fora (em relação ao eixo do corpo)."""
    vs = [o.matrix_world @ v.co for v in o.data.vertices]
    faces = []
    for p in o.data.polygons:
        c = o.matrix_world @ p.center
        n = (o.matrix_world.to_3x3() @ p.normal).normalized()
        radial = Vector((c.x, c.y - float(eixo_y(c.z)), 0.0))
        if radial.length > 1e-6 and n.dot(radial.normalized()) > minimo:
            faces.append(tuple(p.vertices))
    return BVHTree.FromPolygons(vs, faces)

def empurrar(o, folga, obstaculos, iteracoes=10, h=0.02, raio=5, ganho=1.5):
    pos = coords(o)
    t = np.clip((pos[:, 2] - SEM_EMPURRAO) / (COM_EMPURRAO - SEM_EMPURRAO), 0.0, 1.0)
    liberado = (t * t * (3 - 2 * t))[:, None]   # 0 nas pernas, 1 do quadril para cima
    total = np.zeros_like(pos)
    for it in range(iteracoes):
        atual = pos + total
        desl = np.zeros_like(pos)
        for i, p in enumerate(atual):
            for bvh in obstaculos:
                loc, nor, _, dist = bvh.find_nearest(Vector(p), 0.15)
                if loc is None:
                    continue
                s = (Vector(p) - loc).dot(nor)
                if s < folga:
                    d = np.array(nor) * (folga - s)
                    if np.dot(d, d) > np.dot(desl[i], desl[i]):
                        desl[i] = d
        desl *= liberado
        precisa = np.linalg.norm(desl, axis=1)
        print(f"  {o.name} iteração {it}: {int((precisa > 0.002).sum())} vértices furando, "
              f"máx {precisa.max() * 100:.1f} cm")
        if precisa.max() < 0.002:
            break
        total += campo_suave(atual, desl, h, raio) * ganho * liberado
    gravar(o, pos + total)

# Eixo do corpo (centro em Y do tronco por altura), para a saia "por baixo" do manto
cv = coords(corpo)
tronco = cv[np.abs(cv[:, 0]) < 0.22]
alturas = np.arange(0, 2.0, 0.05)
centros = np.array([tronco[np.abs(tronco[:, 2] - z) < 0.025, 1].mean()
                    if (np.abs(tronco[:, 2] - z) < 0.025).any() else 0.0 for z in alturas])
def eixo_y(z):
    return np.interp(z, alturas, centros)

def enfiar_sob_cinto(o, profundidade=0.004):
    """O cinto de couro é parte do corpo: para ele aparecer por cima da saia, o pano que fica na
    altura do cinto entra um pouco para dentro da pele, como se estivesse preso por baixo dele."""
    bvh_c = bvh_mundo(corpo)
    pos = coords(o)
    frente = pos[:, 1] < eixo_y(pos[:, 2])
    baixo = np.where(frente & (np.abs(pos[:, 0]) < MEDALHAO["meia_largura"]), MEDALHAO["baixo"], CINTO_BAIXO)
    t = np.clip((pos[:, 2] - (baixo - CINTO_TRANSICAO)) / CINTO_TRANSICAO, 0.0, 1.0)
    peso = t * t * (3 - 2 * t)
    n = 0
    for i in np.nonzero(peso > 0)[0]:
        loc, nor, _, _ = bvh_c.find_nearest(Vector(pos[i]), 0.2)
        if loc is not None:
            dentro = np.array(loc - nor * profundidade)
            pos[i] = pos[i] * (1 - peso[i]) + dentro * peso[i]
            n += 1
    gravar(o, pos)
    print(f"  {o.name}: {n} vértices presos sob o cinto")

empurrar(saia, FOLGA_SAIA, [bvh_mundo(corpo)])
enfiar_sob_cinto(saia)
empurrar(manto, FOLGA_MANTO, [bvh_mundo(corpo), faces_de_fora(saia, eixo_y)])

def assentar_capuz(o, iteracoes=10, h=0.015, raio=6):
    """Leva cada ponto do capuz para a folga desejada em volta da cabeça (raios saindo do centro da
    cabeça). O deslocamento passa por um campo suave, então as duas faces do pano andam juntas."""
    bvh_c = bvh_mundo(corpo)
    cab = cv[(cv[:, 2] > 1.70) & (np.abs(cv[:, 0]) < 0.12)]
    C = np.array([(cab[:, 0].min() + cab[:, 0].max()) / 2, (cab[:, 1].min() + cab[:, 1].max()) / 2, 1.78])
    pos = coords(o)
    t = np.clip((pos[:, 2] - CAPUZ_INICIO) / (CAPUZ_INTEIRO - CAPUZ_INICIO), 0.0, 1.0)
    peso = (t * t * (3 - 2 * t)) * (np.linalg.norm(pos - C, axis=1) < 0.35)
    idx = np.nonzero(peso > 0)[0]
    total = np.zeros_like(pos)
    alvo = {}
    for it in range(iteracoes):
        atual = pos + total
        desl = np.zeros_like(pos)
        erros = []
        for i in idx:
            v = atual[i] - C
            r = np.linalg.norm(v)
            d = Vector(v / r)
            if d.z < -0.25:
                continue                      # gola e lapelas (abaixo da cabeça) só acompanham os vizinhos
            hit = bvh_c.ray_cast(Vector(C), d, r + 0.3)
            if hit[0] is None:
                continue                      # raio passa pela abertura do rosto sem tocar a cabeça
            vao = r - hit[3]
            if i not in alvo:
                frente = np.clip((-d.y - 0.3) / 0.5, 0.0, 1.0)  # 0 atrás/lados, 1 de frente para o rosto
                folga = CAPUZ_FOLGA + (CAPUZ_FOLGA_ROSTO - CAPUZ_FOLGA) * frente
                alvo[i] = folga + (1 - CAPUZ_FORCA) * max(vao - folga, 0.0)
            erro = alvo[i] - vao
            desl[i] = np.array(d) * erro * peso[i]
            erros.append(abs(erro))
        print(f"  capuz iteração {it}: erro médio {np.mean(erros) * 100:.1f} cm, máx {np.max(erros) * 100:.1f} cm")
        total += campo_suave(atual, desl, h, raio) * peso[:, None]
    gravar(o, pos + total)

assentar_capuz(manto)

# ---------- 3. Lança na mão direita ----------
ossos = arm.data.bones
def osso(n):
    return arm.matrix_world @ ossos[n].head_local
indicador, minimo = osso("CC_Base_R_Index1"), osso("CC_Base_R_Pinky1")
cv_mao = cv[cv[:, 0] < indicador.x + 0.03]                 # dedos da mão direita
palma_z = cv_mao[:, 2].min()
lv = coords(lanca)
cabo = lv[(lv[:, 2] > 0.2) & (lv[:, 2] < 0.55)]
centro_cabo = (cabo.min(0) + cabo.max(0)) / 2
raio_cabo = (cabo[:, 0].max() - cabo[:, 0].min()) / 2
pegada_local = Vector((centro_cabo[0], centro_cabo[1], LANCA_PEGADA))
pegada_mundo = Vector(((indicador.x + minimo.x) / 2 - 0.015,
                       (indicador.y + minimo.y) / 2,
                       palma_z - raio_cabo + 0.004))
M_lanca = (Matrix.Translation(pegada_mundo) @ Matrix.Rotation(math.radians(90), 4, "X")
           @ Matrix.Translation(-pegada_local))
lanca.matrix_world = M_lanca @ lanca.matrix_world
print("LANCA pegada", tuple(round(x, 3) for x in pegada_mundo), "raio do cabo", round(raio_cabo, 3))

bpy.ops.wm.save_mainfile(compress=True)
print("SALVO", bpy.data.filepath)
