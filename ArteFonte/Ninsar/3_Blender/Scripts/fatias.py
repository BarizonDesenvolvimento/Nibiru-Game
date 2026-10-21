# Mede corpo e peças em fatias horizontais (para calcular o encaixe).
# Para cada altura: faixa em Y (frente = Y menor) do corpo (só tronco, |x| < 0.22) e das peças.
# Uso: blender -b Ninsar_Final.blend -P Scripts/fatias.py
import bpy
from collections import defaultdict

PASSO = 0.05
def fatias(nome, xmax=10.0):
    o = bpy.data.objects[nome]
    dg = bpy.context.evaluated_depsgraph_get()
    me = o.evaluated_get(dg).to_mesh()
    d = defaultdict(list)
    for v in me.vertices:
        p = o.matrix_world @ v.co
        if abs(p.x) < xmax:
            d[round(p.z / PASSO)].append(p)
    return d

corpo = fatias("Ninsar_Corpo", 0.22)
pecas = {n: fatias(n) for n in ("Ninsar_Manto", "Ninsar_Saia")}
centro = {n: fatias(n, 0.06) for n in ("Ninsar_Manto", "Ninsar_Saia")}

def faixa(ps, eixo):
    if not ps:
        return "      --      "
    vs = [p[eixo] for p in ps]
    return f"{min(vs):+.3f}..{max(vs):+.3f}"

print("ALT   | corpo Y          corpo X          | manto Y(todo)    manto Y(|x|<6cm) manto X          | saia Y           saia Y(|x|<6cm)  saia X")
for k in sorted(set(corpo) | set(pecas["Ninsar_Manto"]) | set(pecas["Ninsar_Saia"]), reverse=True):
    linha = f"{k*PASSO:5.2f} | {faixa(corpo.get(k), 1)} {faixa(corpo.get(k), 0)} |"
    for n in ("Ninsar_Manto", "Ninsar_Saia"):
        linha += f" {faixa(pecas[n].get(k), 1)} {faixa(centro[n].get(k), 1)} {faixa(pecas[n].get(k), 0)} |"
    print("FATIA", linha)
