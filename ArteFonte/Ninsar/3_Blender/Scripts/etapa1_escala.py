# Etapa 1a: cria Ninsar_Final.blend a partir de Ninsar_Montagem.blend (que fica intacto).
# - Corrige a escala: a malha do corpo já mede 1,90 m por dentro; só a Armature estava com 0,01.
# - Aumenta as peças na mesma proporção e aplica rotação e escala em todas (escala 1 em tudo).
# - Material do corpo com as texturas PBR do Meshy (mesmos UVs do corpo do AccuRig).
# - Tira as texturas das peças de dentro do .blend (pasta Texturas/), para o arquivo ficar leve.
# Uso: blender -b Ninsar_Montagem.blend -P Scripts/etapa1_escala.py
import bpy, os
from mathutils import Matrix

PASTA = os.path.dirname(bpy.data.filepath)
DESTINO = os.path.join(PASTA, "Ninsar_Final.blend")
TEX_CORPO = "//../1_Meshy/Corpo/Ninsar Corpo/Meshy_AI_Gilded_Oracle_1021022223_texture"
PECAS = {"Ninsar_Manto": "Manto", "Ninsar_Saia": "Saia", "Ninsar_Lanca": "Lanca"}

arm = bpy.data.objects["Armature"]
corpo = bpy.data.objects["default"]
corpo.name = "Ninsar_Corpo"
fator = 1.0 / arm.scale.x
arm.scale = (1, 1, 1)

# Pose de repouso: a ação "0_T-Pose" do AccuRig muda um pouco os braços. O encaixe, os pesos e o FBX
# usam a pose de repouso (a malha como foi modelada). A ação fica guardada, sem uso.
if arm.animation_data and arm.animation_data.action:
    arm.animation_data.action.use_fake_user = True
    arm.animation_data.action = None
for pb in arm.pose.bones:
    pb.location, pb.rotation_quaternion, pb.rotation_euler, pb.scale = (0, 0, 0), (1, 0, 0, 0), (0, 0, 0), (1, 1, 1)

# Peças: mesma ampliação em volta da origem do mundo, depois aplica rotação e escala na malha.
for nome in PECAS:
    o = bpy.data.objects[nome]
    o.matrix_world = Matrix.Scale(fator, 4) @ o.matrix_world
    loc, rot, esc = o.matrix_world.decompose()
    o.data.transform(Matrix.LocRotScale(None, rot, esc))
    o.matrix_world = Matrix.Translation(loc)

# Material do corpo
def textura(nt, caminho, cor):
    n = nt.nodes.new("ShaderNodeTexImage")
    n.image = bpy.data.images.load(caminho, check_existing=True)
    n.image.colorspace_settings.name = "sRGB" if cor else "Non-Color"
    return n

mat = bpy.data.materials.new("Ninsar_Corpo")
nt = mat.node_tree
nt.nodes.clear()
bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
nt.links.new(bsdf.outputs["BSDF"], nt.nodes.new("ShaderNodeOutputMaterial").inputs["Surface"])
nt.links.new(textura(nt, TEX_CORPO + ".png", True).outputs["Color"], bsdf.inputs["Base Color"])
nt.links.new(textura(nt, TEX_CORPO + "_metallic.png", False).outputs["Color"], bsdf.inputs["Metallic"])
nt.links.new(textura(nt, TEX_CORPO + "_roughness.png", False).outputs["Color"], bsdf.inputs["Roughness"])
nmap = nt.nodes.new("ShaderNodeNormalMap")
nt.links.new(textura(nt, TEX_CORPO + "_normal.png", False).outputs["Color"], nmap.inputs["Color"])
nt.links.new(nmap.outputs["Normal"], bsdf.inputs["Normal"])
corpo.data.materials.clear()
corpo.data.materials.append(mat)

# Texturas das peças para fora do .blend, com nome pelo papel no material
os.makedirs(os.path.join(PASTA, "Texturas"), exist_ok=True)
for nome, curto in PECAS.items():
    m = bpy.data.objects[nome].data.materials[0]
    m.name = "Ninsar_" + curto
    for n in m.node_tree.nodes:
        if n.type != "TEX_IMAGE" or not n.image or not n.image.packed_file:
            continue
        alvo = n.outputs["Color"].links[0].to_socket
        papel = "Normal" if alvo.node.type == "NORMAL_MAP" else alvo.name.replace(" ", "")
        ext = ".jpg" if n.image.file_format == "JPEG" else ".png"
        arquivo = f"Texturas/{curto}_{papel}{ext}"
        with open(os.path.join(PASTA, arquivo), "wb") as f:
            f.write(n.image.packed_file.data)
        n.image.filepath = "//" + arquivo
        n.image.name = f"{curto}_{papel}"
        n.image.unpack(method="REMOVE")
        n.image.reload()

for m in list(bpy.data.materials):
    if m.users == 0:
        bpy.data.materials.remove(m)
bpy.data.orphans_purge(do_recursive=True)

bpy.ops.wm.save_as_mainfile(filepath=DESTINO, compress=True, relative_remap=True)
print("SALVO", DESTINO, "fator", fator)
