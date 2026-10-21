# Render de conferência da lança: baixa o braço direito, dobra o antebraço para a frente e leva a lança
# junto com a mão (só neste render; não salva o .blend). Esconde o manto, que ainda não tem pesos.
# Uso: blender -b Ninsar_Final.blend -P Scripts/renderizar_pose_lanca.py
import bpy, math, os, sys
from mathutils import Matrix

arm = bpy.data.objects["Armature"]
lanca = bpy.data.objects["Ninsar_Lanca"]

def girar(nome, eixo, graus):
    pb = arm.pose.bones[nome]
    bpy.context.view_layer.update()
    cabeca = pb.head.copy()
    R = Matrix.Translation(cabeca) @ Matrix.Rotation(math.radians(graus), 4, eixo) @ Matrix.Translation(-cabeca)
    pb.matrix = R @ pb.matrix
    bpy.context.view_layer.update()

mao = arm.pose.bones["CC_Base_R_Hand"]
antes = arm.matrix_world @ mao.matrix
girar("CC_Base_R_Upperarm", "Y", -70)   # braço para baixo
girar("CC_Base_R_Forearm", "X", -80)    # antebraço para a frente
depois = arm.matrix_world @ mao.matrix
lanca.matrix_world = depois @ antes.inverted() @ lanca.matrix_world

sys.argv = [sys.argv[0], "--", "03_PoseLanca", "Ninsar_Manto"]
exec(open(os.path.join(os.path.dirname(__file__), "renderizar_vistas.py"), encoding="utf-8").read())
