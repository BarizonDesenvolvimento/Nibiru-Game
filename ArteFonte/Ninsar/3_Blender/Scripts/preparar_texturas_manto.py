# Prepara as texturas do manto novo a partir das imagens do dono (ArteFonte/Ninsar/Texturas), sem
# alterar os originais. Saída em Texturas/Processadas:
#   Manto_Tecido_Cor/Normal/Ouro.png  tecido de fundo, repetível nos dois sentidos
#   Manto_Faixa_Cor/Normal/Ouro.png   faixa de bordado: só a área bordada (linha dourada de cima até a de
#                                     baixo), com ciclos inteiros do padrão, repetível na horizontal
# "Ouro" é a máscara do fio dourado (branco = ouro), usada para o brilho metálico.
# O verde das duas vai para o verdete do manto do Meshy (Texturas/Manto_BaseColor.jpg); o dourado fica.
# Uso: blender -b -P Scripts/preparar_texturas_manto.py
import bpy, os
import numpy as np

AQUI = os.path.dirname(os.path.abspath(__file__))
ORIGEM = os.path.normpath(os.path.join(AQUI, "..", "..", "Texturas"))
SAIDA = os.path.join(ORIGEM, "Processadas")
REF_COR = os.path.normpath(os.path.join(AQUI, "..", "Texturas", "Manto_BaseColor.jpg"))
os.makedirs(SAIDA, exist_ok=True)

def carregar(caminho):
    im = bpy.data.images.load(caminho)
    im.colorspace_settings.name = "sRGB"
    w, h = im.size
    a = np.empty(w * h * 4, dtype=np.float32)
    im.pixels.foreach_get(a)
    bpy.data.images.remove(im)
    return a.reshape(h, w, 4)[::-1, :, :3].copy()     # linha 0 = topo; valores sRGB 0..1

def salvar(rgb, nome, dados=False):
    h, w = rgb.shape[:2]
    if rgb.ndim == 2:
        rgb = np.repeat(rgb[..., None], 3, axis=2)
    im = bpy.data.images.new(nome, w, h, alpha=False, float_buffer=False)
    im.colorspace_settings.name = "Non-Color" if dados else "sRGB"
    px = np.ones((h, w, 4), dtype=np.float32)
    px[..., :3] = np.clip(rgb, 0, 1)
    im.pixels.foreach_set(px[::-1].ravel())
    im.filepath_raw = os.path.join(SAIDA, nome + ".png")
    im.file_format = "PNG"
    im.save()
    bpy.data.images.remove(im)
    print("SALVO", nome, w, "x", h)

# ---- cor: sRGB <-> Lab ----
def srgb_lin(c):
    return np.where(c <= 0.04045, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
def lin_srgb(c):
    c = np.clip(c, 0, 1)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * c ** (1 / 2.4) - 0.055)
M = np.array([[0.4124, 0.3576, 0.1805], [0.2126, 0.7152, 0.0722], [0.0193, 0.1192, 0.9505]])
BRANCO = np.array([0.9505, 1.0, 1.089])
def lab(rgb):
    xyz = srgb_lin(rgb) @ M.T / BRANCO
    f = np.where(xyz > 0.008856, np.cbrt(xyz), 7.787 * xyz + 16 / 116)
    return np.stack([116 * f[..., 1] - 16, 500 * (f[..., 0] - f[..., 1]), 200 * (f[..., 1] - f[..., 2])], -1)
def rgb_de_lab(L):
    fy = (L[..., 0] + 16) / 116
    fx, fz = fy + L[..., 1] / 500, fy - L[..., 2] / 200
    f = np.stack([fx, fy, fz], -1)
    xyz = np.where(f > 0.2069, f ** 3, (f - 16 / 116) / 7.787) * BRANCO
    return lin_srgb(xyz @ np.linalg.inv(M).T)

def mascara_ouro(rgb):
    """Fio dourado: vermelho acima do verde (o fundo é verde), com brilho suficiente."""
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    t1 = np.clip(((r - g) + 0.01) / 0.06, 0, 1)
    t2 = np.clip((np.maximum(r, g) - 0.18) / 0.12, 0, 1)
    return (t1 * t1 * (3 - 2 * t1)) * t2

def borrar(a, raio, eixos_circulares=(True, True)):
    if raio <= 0:
        return a
    k = np.exp(-0.5 * (np.arange(-3 * raio, 3 * raio + 1) / raio) ** 2)
    k /= k.sum()
    for eixo, circ in zip((0, 1), eixos_circulares):
        if circ:
            a = sum(w * np.roll(a, s, axis=eixo) for s, w in zip(range(-3 * raio, 3 * raio + 1), k))
        else:
            pad = [(0, 0)] * a.ndim
            pad[eixo] = (3 * raio, 3 * raio)
            p = np.pad(a, pad, mode="edge")
            a = sum(w * np.take(p, np.arange(3 * raio + s, 3 * raio + s + a.shape[eixo]), axis=eixo)
                    for s, w in zip(range(-3 * raio, 3 * raio + 1), k))
    return a

def periodo(img, minimo, maximo, eixo):
    """Período do padrão ao longo de um eixo, por autocorrelação (média das linhas/colunas)."""
    g = img.mean(-1)
    g = g - g.mean(axis=eixo, keepdims=True)
    F = np.fft.rfft(g, n=2 * g.shape[eixo], axis=eixo)
    ac = np.fft.irfft(F * np.conj(F), axis=eixo)
    ac = ac.mean(axis=1 - eixo)
    ac = ac / ac[0]
    lag = minimo + int(np.argmax(ac[minimo:maximo]))
    # refinamento sub-pixel não é preciso: cortamos em pixels inteiros
    return lag, float(ac[lag])

def emenda(img, eixo):
    """Diferença na emenda (última -> primeira linha/coluna) comparada à diferença interna média."""
    a = np.moveaxis(img, eixo, 0)
    interna = np.abs(np.diff(a, axis=0)).mean()
    borda = np.abs(a[-1] - a[0]).mean()
    return borda / interna

def repetivel(img, eixo, P, mistura=48, conteudo=None):
    """Corta um número inteiro de períodos P no eixo e mistura a emenda com o que vem depois do corte.
    Se 'conteudo' (quanto desenho há em cada posição do eixo) vier, o corte cai onde há menos desenho."""
    a = np.moveaxis(img, eixo, 0)
    L = a.shape[0]
    n = (L - mistura) // P
    comp = n * P
    folga = L - comp - mistura
    if conteudo is not None and folga > 0:
        c = np.convolve(conteudo, np.ones(mistura) / mistura, mode="same")
        x0 = int(np.argmin([c[x] + c[x + comp] for x in range(folga + 1)]))
    else:
        x0 = folga // 2
    print(f"  corte em {x0}..{x0 + comp}")
    out = a[x0:x0 + comp].copy()
    depois = a[x0 + comp:x0 + comp + mistura]          # continuação natural do fim
    t = np.linspace(0, 1, mistura)[:, None, None] if a.ndim == 3 else np.linspace(0, 1, mistura)[:, None]
    t = t * t * (3 - 2 * t)
    out[:mistura] = depois * (1 - t) + out[:mistura] * t
    return np.moveaxis(out, 0, eixo), n, comp

def normal_de_altura(alt, forca, circular):
    """Normal map (tangente, padrão OpenGL: verde para cima) a partir de um mapa de altura 0..1."""
    def deriv(a, eixo, circ):
        if circ:
            return (np.roll(a, -1, axis=eixo) - np.roll(a, 1, axis=eixo)) / 2
        return np.gradient(a, axis=eixo)
    dx = deriv(alt, 1, circular[1])
    dy = deriv(alt, 0, circular[0])
    n = np.stack([-dx * forca, dy * forca, np.ones_like(alt)], -1)   # linha 0 = topo, então +y é -linha
    n /= np.linalg.norm(n, axis=-1, keepdims=True)
    return n * 0.5 + 0.5

# ---------------- alvo de cor: verde do manto do Meshy ----------------
ref = carregar(REF_COR)
ref_lab = lab(ref)
fundo_ref = (mascara_ouro(ref) < 0.05) & (ref[..., 1] > ref[..., 0] + 0.01)   # só o verde de fato
alvo = np.median(ref_lab[fundo_ref], axis=0)
print("ALVO verdete (Lab)", np.round(alvo, 1), "rgb", np.round(rgb_de_lab(alvo[None, None])[0, 0], 3))

def tingir(rgb):
    """Leva a média do fundo verde para o alvo (Lab), mantendo as variações; o ouro quase não muda."""
    L = lab(rgb)
    m = mascara_ouro(rgb)
    fundo = m < 0.1
    media = L[fundo].mean(0)
    desl = alvo - media
    peso = (1 - borrar(m, 2))[..., None]
    print("  fundo antes (Lab)", np.round(media, 1), "deslocamento", np.round(desl, 1))
    return rgb_de_lab(L + desl * peso), m

# Os arquivos chegaram com os nomes trocados; escolhe cada um pelo formato (a faixa é bem mais larga).
arqs = [os.path.join(ORIGEM, f) for f in ("Ninsar_Tecido_Manto.png", "Ninsar_Faixa_Bordado.png")]
imgs = {a: carregar(a) for a in arqs}
faixa_arq = max(imgs, key=lambda a: imgs[a].shape[1] / imgs[a].shape[0])
tecido_arq = [a for a in imgs if a != faixa_arq][0]
print("FAIXA vem de", os.path.basename(faixa_arq), imgs[faixa_arq].shape[1::-1])
print("TECIDO vem de", os.path.basename(tecido_arq), imgs[tecido_arq].shape[1::-1])

# ---------------- faixa ----------------
fx = imgs[faixa_arq]
m = mascara_ouro(fx)
perfil = m.mean(1)                       # fração de ouro em cada linha
linhas = np.nonzero(perfil > 0.45)[0]    # linhas douradas contínuas (as de cima e as de baixo)
topo, base = linhas.min(), linhas.max()
print(f"FAIXA linhas douradas: topo {topo}, base {base} (altura {base - topo + 1}); ouro máx por linha {perfil.max():.2f}")
fx = fx[max(topo - 2, 0):base + 3]
meio = fx[fx.shape[0] // 4: 3 * fx.shape[0] // 4]
P, corr = periodo(meio, fx.shape[1] // 4, fx.shape[1] - 64, eixo=1)
P2, corr2 = periodo(meio, 200, P - 100, eixo=1)
print(f"FAIXA período do ciclo completo {P}px (correlação {corr:.2f}); meio-ciclo provável {P2}px ({corr2:.2f})")
fx, n, comp = repetivel(fx, 1, P, conteudo=mascara_ouro(meio).mean(0))
print(f"FAIXA recorte: {n} ciclo(s), {comp}px; emenda/ interna = {emenda(fx, 1):.2f}")
fx_cor, fx_m = tingir(fx)
alt = borrar(fx_m, 1, (False, True)) * 0.8 + borrar(fx.mean(-1), 1, (False, True)) * 0.2
salvar(fx_cor, "Manto_Faixa_Cor")
salvar(normal_de_altura(alt, 6.0, (False, True)), "Manto_Faixa_Normal", dados=True)
salvar(fx_m, "Manto_Faixa_Ouro", dados=True)

# ---------------- tecido ----------------
tc = imgs[tecido_arq]
ex, ey = emenda(tc, 1), emenda(tc, 0)
print(f"TECIDO emenda/interna: horizontal {ex:.2f}, vertical {ey:.2f} (perto de 1 = sem emenda)")
# A medida de emenda não pega o desalinhamento da treliça (o tecido tem muito ruído de trama); a prévia
# 2x2 mostrou a quebra dos losangos. Então o corte em losangos inteiros é sempre feito.
for eixo, e in ((1, ex), (0, ey)):
    if True:
        P, corr = periodo(tc, 60, tc.shape[eixo] // 2, eixo)
        tc, n, comp = repetivel(tc, eixo, P, mistura=96, conteudo=mascara_ouro(tc).mean(1 - eixo))
        print(f"TECIDO eixo {'x' if eixo else 'y'}: período {P}px (corr {corr:.2f}), {n} períodos, {comp}px; "
              f"emenda agora {emenda(tc, eixo):.2f}")
tc_cor, tc_m = tingir(tc)
alt = borrar(tc_m, 1) * 0.7 + borrar(tc.mean(-1), 1) * 0.3
salvar(tc_cor, "Manto_Tecido_Cor")
salvar(normal_de_altura(alt, 4.0, (True, True)), "Manto_Tecido_Normal", dados=True)
salvar(tc_m, "Manto_Tecido_Ouro", dados=True)

# Prévias para conferir as emendas (tecido 2x2, faixa 3 vezes), na pasta de renders (fora do Git)
previas = os.path.join(AQUI, "..", "Renders")
os.makedirs(previas, exist_ok=True)
SAIDA_ORIG = SAIDA
SAIDA = previas
salvar(np.tile(tc_cor, (2, 2, 1)), "Previa_Tecido_2x2")
salvar(np.tile(fx_cor, (1, 3, 1)), "Previa_Faixa_3x")
SAIDA = SAIDA_ORIG
