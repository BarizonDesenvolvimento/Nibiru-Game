# Mede o vão entre o capuz e a cabeça: raios saindo do centro da cabeça em várias direções.
# Uso: blender -b Ninsar_Final.blend -P Scripts/medir_capuz.py
import bpy, math
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree

def bvh(nome):
    o = bpy.data.objects[nome]
    me = o.evaluated_get(bpy.context.evaluated_depsgraph_get()).to_mesh()
    return BVHTree.FromPolygons([o.matrix_world @ v.co for v in me.vertices], [tuple(p.vertices) for p in me.polygons])

corpo, manto = bvh("Ninsar_Corpo"), bvh("Ninsar_Manto")
cv = np.array([v.co[:] for v in bpy.data.objects["Ninsar_Corpo"].data.vertices])
cab = cv[(cv[:, 2] > 1.70) & (np.abs(cv[:, 0]) < 0.12)]
C = Vector(((cab[:, 0].min() + cab[:, 0].max()) / 2, (cab[:, 1].min() + cab[:, 1].max()) / 2, 1.78))
print("CABECA centro", tuple(round(x, 3) for x in C), "topo", round(cab[:, 2].max(), 3),
      "x", round(cab[:, 0].min(), 3), round(cab[:, 0].max(), 3), "y", round(cab[:, 1].min(), 3), round(cab[:, 1].max(), 3))
# elevação (0 = horizontal, 90 = para cima), azimute (0 = trás +Y, 90 = lado esquerdo +X, 180 = frente)
for elev in (90, 60, 30, 0, -30):
    linha = []
    for az in (0, 45, 90, 135, 180):
        d = Vector((math.sin(math.radians(az)) * math.cos(math.radians(elev)),
                    math.cos(math.radians(az)) * math.cos(math.radians(elev)),
                    math.sin(math.radians(elev))))
        hc = corpo.ray_cast(C, d, 1.0)
        dc = hc[3] if hc[0] else float("nan")
        hm = manto.ray_cast(C + d * (dc + 0.001 if hc[0] else 0), d, 1.0)
        dm = hm[3] if hm[0] else float("nan")
        linha.append(f"az{az:3d}: vão {dm * 100:5.1f}cm")
        if elev == 90:
            break
    print(f"ELEV {elev:3d} | " + " | ".join(linha))
