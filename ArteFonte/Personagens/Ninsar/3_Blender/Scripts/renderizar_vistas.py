# Renderiza a Ninsar de frente, de lado (direito dela), de costas e de 3/4 (frente e lado direito), com câmera ortográfica.
# Não salva o .blend. As imagens vão para Renders/<prefixo>_Frente.png etc.
# Uso: blender -b Ninsar_Final.blend -P Scripts/renderizar_vistas.py -- <prefixo> [objetos escondidos...]
import bpy, os, sys, math
from mathutils import Vector

args = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
prefixo = args[0] if args else "Vista"
esconder = set(args[1:])
pasta = os.path.join(os.path.dirname(bpy.data.filepath), "Renders")
os.makedirs(pasta, exist_ok=True)

cena = bpy.context.scene
for nome in ("BLENDER_EEVEE", "BLENDER_EEVEE_NEXT"):
    try:
        cena.render.engine = nome
        break
    except TypeError:
        pass
cena.render.resolution_x, cena.render.resolution_y = 900, 1500
cena.render.film_transparent = False
cena.view_settings.view_transform = "AgX"

for o in bpy.data.objects:
    if o.type == "MESH":
        o.hide_render = o.name in esconder

mundo = bpy.data.worlds.new("Fundo") if not cena.world else cena.world
mundo.color = (0.18, 0.18, 0.2)
if mundo.node_tree and "Background" in mundo.node_tree.nodes:
    mundo.node_tree.nodes["Background"].inputs[0].default_value = (0.18, 0.18, 0.2, 1)
    mundo.node_tree.nodes["Background"].inputs[1].default_value = 1.0
cena.world = mundo

def luz(nome, energia, rot):
    d = bpy.data.lights.new(nome, "SUN")
    d.energy = energia
    o = bpy.data.objects.new(nome, d)
    o.rotation_euler = [math.radians(a) for a in rot]
    cena.collection.objects.link(o)

luz("Chave", 3.0, (55, 0, -35))
luz("Recorte", 2.0, (60, 0, 160))
luz("Preenche", 1.0, (70, 0, 60))

cam_d = bpy.data.cameras.new("Cam")
cam_d.type = "ORTHO"
cam_d.ortho_scale = 2.1
cam = bpy.data.objects.new("Cam", cam_d)
cena.collection.objects.link(cam)
cena.camera = cam

# A Ninsar olha para -Y. Lado direito dela = -X.
vistas = {"Frente": (Vector((0, -5, 1.0)), (90, 0, 0)),
          "Lado": (Vector((-5, 0, 1.0)), (90, 0, -90)),
          "Costas": (Vector((0, 5, 1.0)), (90, 0, 180)),
          "TresQuartos": (Vector((-3.54, -3.54, 1.0)), (90, 0, -45))}
for nome, (pos, rot) in vistas.items():
    cam.location = pos
    cam.rotation_euler = [math.radians(a) for a in rot]
    cena.render.filepath = os.path.join(pasta, f"{prefixo}_{nome}.png")
    bpy.ops.render.render(write_still=True)
    print("RENDER", cena.render.filepath)
