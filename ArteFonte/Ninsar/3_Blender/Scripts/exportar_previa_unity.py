# Prévia na Unity: exporta a Ninsar com corpo, saia (pesos simples, sem física) e lança presa à mão
# direita, mais as texturas no formato do URP. Não salva o .blend.
# Saída: Assets/_Nibiru/Art/Personagens/Ninsar/Ninsar_Previa.fbx e Texturas/
#   *_BaseColor.png, *_Normal.png, *_MetalSmooth.png (R = metal, A = lisura = 1 - rugosidade)
# Uso: blender -b Ninsar_Final.blend -P Scripts/exportar_previa_unity.py
import bpy, os
import numpy as np

AQUI = os.path.dirname(bpy.data.filepath)
PROJETO = os.path.normpath(os.path.join(AQUI, "..", "..", ".."))
DESTINO = os.path.join(PROJETO, "Assets", "_Nibiru", "Art", "Personagens", "Ninsar")
TEX = os.path.join(DESTINO, "Texturas")
os.makedirs(TEX, exist_ok=True)

arm = bpy.data.objects["Armature"]
corpo = bpy.data.objects["Ninsar_Corpo"]
saia = bpy.data.objects["Ninsar_Saia"]
lanca = bpy.data.objects["Ninsar_Lanca"]
for o in list(bpy.data.objects):
    if o not in (arm, corpo, saia, lanca):
        bpy.data.objects.remove(o)

# ---------- Saia: pesos simples ----------
# Em cima, toda na pelve; descendo, uma parte passa para coxas e canelas (lado pelo x, com mistura no
# meio para não rasgar entre as pernas). As pernas nunca levam mais que 70%: a saia longa fica firme.
def suave(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)
co = np.empty(len(saia.data.vertices) * 3)
saia.data.vertices.foreach_get("co", co)
co = co.reshape(-1, 3)
m = np.array(saia.matrix_world)
co = co @ m[:3, :3].T + m[:3, 3]
x, z = co[:, 0], co[:, 2]
esq = suave(-0.10, 0.10, x)                 # lado esquerdo dela = +x
perna = suave(1.05, 0.85, z) * 0.7
canela = suave(0.60, 0.40, z) * 0.5
pesos = {
    "CC_Base_Pelvis": 1 - perna,
    "CC_Base_L_Thigh": perna * esq * (1 - canela),
    "CC_Base_L_Calf": perna * esq * canela,
    "CC_Base_R_Thigh": perna * (1 - esq) * (1 - canela),
    "CC_Base_R_Calf": perna * (1 - esq) * canela,
}
saia.vertex_groups.clear()
for nome, w in pesos.items():
    g = saia.vertex_groups.new(name=nome)
    for valor in np.unique(np.round(w, 3)):
        if valor > 0:
            g.add([int(i) for i in np.nonzero(np.round(w, 3) == valor)[0]], float(valor), "REPLACE")
saia.modifiers.clear()
mw = saia.matrix_world.copy()
saia.parent = arm
saia.matrix_world = mw
saia.modifiers.new("Armature", "ARMATURE").object = arm

# ---------- Lança: filha do osso da mão direita ----------
mw = lanca.matrix_world.copy()
lanca.parent = arm
lanca.parent_type = "BONE"
lanca.parent_bone = "CC_Base_R_Hand"
lanca.matrix_world = mw

# ---------- Texturas ----------
def ler(img):
    w, h = img.size
    a = np.empty(w * h * 4, dtype=np.float32)
    img.pixels.foreach_get(a)
    return a.reshape(h, w, 4)

def reduzir(a, alvo):
    while a.shape[0] > alvo:
        a = a.reshape(a.shape[0] // 2, 2, a.shape[1] // 2, 2, -1).mean((1, 3))
    return a

def gravar(px, caminho, dados):
    h, w = px.shape[:2]
    im = bpy.data.images.new(os.path.basename(caminho), w, h, alpha=True)
    im.colorspace_settings.name = "Non-Color" if dados else "sRGB"
    im.pixels.foreach_set(px.astype(np.float32).ravel())
    im.filepath_raw = caminho
    im.file_format = "PNG"
    im.save()
    bpy.data.images.remove(im)
    print("TEXTURA", os.path.relpath(caminho, PROJETO), w, "x", h)

def imagem_do_material(mat, entrada):
    bsdf = next(n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    no = bsdf.inputs[entrada].links[0].from_node
    if no.type == "NORMAL_MAP":
        no = no.inputs["Color"].links[0].from_node
    img = no.image
    img.colorspace_settings.name = "Non-Color" if entrada != "Base Color" else "sRGB"
    return img

for obj, nome in ((corpo, "Corpo"), (saia, "Saia"), (lanca, "Lanca")):
    mat = obj.data.materials[0]
    mat.name = "Ninsar_" + nome            # a Unity troca pelo material URP com este nome
    cor = reduzir(ler(imagem_do_material(mat, "Base Color")), 2048)
    gravar(cor, os.path.join(TEX, f"{nome}_BaseColor.png"), False)
    nor = reduzir(ler(imagem_do_material(mat, "Normal")), 2048)
    gravar(nor, os.path.join(TEX, f"{nome}_Normal.png"), True)
    met = reduzir(ler(imagem_do_material(mat, "Metallic")), 2048)
    rug = reduzir(ler(imagem_do_material(mat, "Roughness")), 2048)
    ms = np.zeros_like(met)
    ms[..., 0] = ms[..., 1] = ms[..., 2] = met[..., 0]
    ms[..., 3] = 1.0 - rug[..., 0]
    gravar(ms, os.path.join(TEX, f"{nome}_MetalSmooth.png"), True)

# ---------- FBX ----------
for o in bpy.context.view_layer.objects:
    o.select_set(o in (arm, corpo, saia, lanca))
bpy.context.view_layer.objects.active = arm
fbx = os.path.join(DESTINO, "Ninsar_Previa.fbx")
bpy.ops.export_scene.fbx(filepath=fbx, use_selection=True, object_types={"ARMATURE", "MESH"},
                         apply_scale_options="FBX_SCALE_ALL", add_leaf_bones=False, bake_anim=False,
                         mesh_smooth_type="FACE", use_mesh_modifiers=False, path_mode="STRIP")
print("FBX", os.path.relpath(fbx, PROJETO), round(os.path.getsize(fbx) / 1e6, 1), "MB")
