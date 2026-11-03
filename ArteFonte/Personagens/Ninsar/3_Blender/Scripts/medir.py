# Imprime medidas da Ninsar: ossos principais (posição no mundo) e caixas das peças.
# Uso: blender -b Ninsar_Final.blend -P Scripts/medir.py
import bpy
from mathutils import Vector

def caixa(o):
    pts = [o.matrix_world @ v.co for v in o.data.vertices] if o.type == "MESH" else \
          [o.matrix_world @ Vector(c) for c in o.bound_box]
    mn = Vector([min(p[i] for p in pts) for i in range(3)])
    mx = Vector([max(p[i] for p in pts) for i in range(3)])
    return mn, mx

def r(v):
    return tuple(round(x, 3) for x in v)

arm = bpy.data.objects["Armature"]
for o in bpy.data.objects:
    if o.type in {"MESH", "ARMATURE"}:
        mn, mx = caixa(o)
        print(f"{o.name:14} esc={r(o.scale)} min={r(mn)} max={r(mx)} tam={r(mx - mn)}")
for nome in ["CC_Base_Hip", "CC_Base_Waist", "CC_Base_Spine02", "CC_Base_NeckTwist01", "CC_Base_Head",
             "CC_Base_L_Clavicle", "CC_Base_L_Upperarm", "CC_Base_R_Clavicle", "CC_Base_R_Upperarm",
             "CC_Base_R_Forearm", "CC_Base_R_Hand", "CC_Base_R_Mid1", "CC_Base_R_Thumb1", "CC_Base_R_Index1",
             "CC_Base_L_Thigh", "CC_Base_L_Calf", "CC_Base_L_Foot"]:
    pb = arm.pose.bones[nome]
    print(f"OSSO {nome:22} cabeca={r(arm.matrix_world @ pb.head)} cauda={r(arm.matrix_world @ pb.tail)}")
