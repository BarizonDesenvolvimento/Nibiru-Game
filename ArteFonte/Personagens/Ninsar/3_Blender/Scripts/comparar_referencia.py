# Monta imagens lado a lado: render da Ninsar (esquerda) e a imagem de referência (direita), na mesma altura.
# Saída: Renders/Comparacao_<vista>.png
# Uso: blender -b -P Scripts/comparar_referencia.py -- <prefixo do render>
import bpy, os, sys
import numpy as np

AQUI = os.path.dirname(os.path.abspath(__file__))
RENDERS = os.path.normpath(os.path.join(AQUI, "..", "Renders"))
REF = os.path.normpath(os.path.join(AQUI, "..", "..", "Imagens De Referencia", "Roupas", "Manto com capuz"))
prefixo = sys.argv[sys.argv.index("--") + 1]
PARES = {"Frente": "Ninsar Manto com capuz Frente.png",
         "Lado": "Ninsar Manto com Lado Direito.png",
         "Costas": "Ninsar Manto com capuz Costas.png",
         "TresQuartos": "Ninsar Manto com capuz 4 vistas.png"}

def ler(caminho):
    im = bpy.data.images.load(caminho)
    w, h = im.size
    a = np.empty(w * h * 4, dtype=np.float32)
    im.pixels.foreach_get(a)
    bpy.data.images.remove(im)
    return a.reshape(h, w, 4)

def altura(a, h):
    """Redimensiona (vizinho mais próximo, suficiente para comparação) para a altura h."""
    w = int(round(a.shape[1] * h / a.shape[0]))
    ys = (np.arange(h) * a.shape[0] / h).astype(int)
    xs = (np.arange(w) * a.shape[1] / w).astype(int)
    return a[ys][:, xs]

for vista, ref in PARES.items():
    r = ler(os.path.join(RENDERS, f"{prefixo}_{vista}.png"))
    f = altura(ler(os.path.join(REF, ref)), r.shape[0])
    f[..., 3] = 1.0
    espaco = np.ones((r.shape[0], 20, 4), dtype=np.float32)
    out = np.concatenate([r, espaco, f], axis=1)
    h, w = out.shape[:2]
    im = bpy.data.images.new("comp", w, h, alpha=False)
    im.pixels.foreach_set(out.ravel())
    im.filepath_raw = os.path.join(RENDERS, f"Comparacao_{vista}.png")
    im.file_format = "PNG"
    im.save()
    bpy.data.images.remove(im)
    print("COMPARACAO", vista, w, "x", h)
