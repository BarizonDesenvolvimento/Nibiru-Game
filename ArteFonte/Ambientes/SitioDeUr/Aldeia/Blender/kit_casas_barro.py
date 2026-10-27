# Kit de casas de barro da Aldeia (área-vitrine, B1v Parte 3).
#
# Gera por script as peças das casas em coordenadas do MUNDO da Unity (1 unidade = 1 m) e exporta um FBX por
# grupo. Na Unity, o menu "Nibiru > Arte > Vitrine da Aldeia > 2. Montar vitrine na cena aberta" põe cada FBX na
# origem e dá o material pelo sufixo do nome de cada peça ("__Reboco", "__Madeira"...).
#
# Rodar (o caminho do Blender pode mudar):
#   blender -b --factory-startup -P kit_casas_barro.py -- <pasta dos FBX> <arquivo .blend>
#
# As medidas seguem o blockout (Scripts/Editor/Blockout/SitioDeUrBlockout.cs); a arte não muda caminhos nem
# colisores. Limites que não podem mudar: face interna da parede do fundo da casa do altar em z = -31,5
# (inscrição do Eco) e o vão da frente aberto.
#
# Eixos: o FBX chega na Unity como (x, z, y) do Blender. Por isso tudo aqui é escrito em coordenadas da
# Unity e passa por B() antes de virar vértice. UV em metros: uma unidade de UV = 1 m (a escala fica no material).

import math
import random
import sys

import bmesh
import bpy
from mathutils import Vector

random.seed(7)


def B(x, y, z):
    """Unity (x, y, z) -> Blender (x, z, y)."""
    return Vector((x, z, y))


# ---------- utilidades de malha ----------

def new_object(name, bm):
    bmesh.ops.triangulate(bm, faces=list(bm.faces))
    box_uv(bm)
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    return ob


def box_uv(bm):
    """UV em metros pela direção dominante da normal (projeção de caixa)."""
    uv = bm.loops.layers.uv.verify()
    bm.normal_update()
    for f in bm.faces:
        n = f.normal
        ax = max(range(3), key=lambda i: abs(n[i]))
        for loop in f.loops:
            p = loop.vert.co
            if ax == 2:      # chão e teto (y da Unity)
                loop[uv].uv = (p.x, p.y)
            elif ax == 0:    # paredes viradas para x
                loop[uv].uv = (p.y * (1 if n.x > 0 else -1), p.z)
            else:            # paredes viradas para z
                loop[uv].uv = (p.x * (-1 if n.y > 0 else 1), p.z)


def fillet(points, radius, segments=4):
    """Arredonda os cantos de um polígono 2D (x, z) da Unity. radius pode ser número ou lista por canto."""
    out = []
    n = len(points)
    for i in range(n):
        r = radius[i] if isinstance(radius, (list, tuple)) else radius
        p = Vector(points[i])
        a = Vector(points[i - 1])
        b = Vector(points[(i + 1) % n])
        d1 = (a - p)
        d2 = (b - p)
        l1, l2 = d1.length, d2.length
        d1.normalize()
        d2.normalize()
        cos = max(-1.0, min(1.0, d1.dot(d2)))
        theta = math.acos(cos)
        if r <= 0 or theta < 1e-3 or theta > math.pi - 1e-3:
            out.append(tuple(p))
            continue
        t = min(r / math.tan(theta / 2), l1 * 0.45, l2 * 0.45)
        r = t * math.tan(theta / 2)
        t1 = p + d1 * t
        t2 = p + d2 * t
        bis = (d1 + d2).normalized()
        c = p + bis * (r / math.sin(theta / 2))
        a1 = math.atan2(t1.y - c.y, t1.x - c.x)
        a2 = math.atan2(t2.y - c.y, t2.x - c.x)
        da = a2 - a1
        while da > math.pi:
            da -= 2 * math.pi
        while da < -math.pi:
            da += 2 * math.pi
        for s in range(segments + 1):
            ang = a1 + da * s / segments
            out.append((c.x + r * math.cos(ang), c.y + r * math.sin(ang)))
    return out


def prism(bm, poly, y0, y1, top_jitter=0.0):
    """Extrusão de um polígono (x, z) da Unity entre as alturas y0 e y1."""
    bottom = [bm.verts.new(B(x, y0, z)) for x, z in poly]
    top = [bm.verts.new(B(x, y1 + (random.uniform(-top_jitter, top_jitter) if top_jitter else 0), z)) for x, z in poly]
    made = [bm.faces.new(bottom), bm.faces.new(top)]
    n = len(poly)
    for i in range(n):
        j = (i + 1) % n
        made.append(bm.faces.new((bottom[i], bottom[j], top[j], top[i])))
    bmesh.ops.recalc_face_normals(bm, faces=made)


def box(bm, x0, x1, y0, y1, z0, z1):
    prism(bm, [(x0, z0), (x1, z0), (x1, z1), (x0, z1)], y0, y1)


def ring(bm, x0, x1, z0, z1, width, y0, y1, radius=0.0, top_jitter=0.0):
    """Mureta em volta de um retângulo (parapeito), com a borda externa arredondada."""
    for (a0, a1, b0, b1) in (
        (x0, x1, z0, z0 + width), (x0, x1, z1 - width, z1),
        (x0, x0 + width, z0 + width, z1 - width), (x1 - width, x1, z0 + width, z1 - width)):
        prism(bm, fillet([(a0, b0), (a1, b0), (a1, b1), (a0, b1)], 0.05, 2), y0, y1, top_jitter)


def log_x(bm, x0, x1, y, z, r, sides=8):
    """Tronco (viga de palmeira) ao longo de x."""
    r = r * random.uniform(0.85, 1.15)
    dy, dz = random.uniform(-0.02, 0.02), random.uniform(-0.02, 0.02)
    ring0, ring1 = [], []
    rot = random.uniform(0, math.pi)
    for s in range(sides):
        a = rot + 2 * math.pi * s / sides
        rr = r * random.uniform(0.9, 1.05)
        ring0.append(bm.verts.new(B(x0, y + rr * math.sin(a), z + rr * math.cos(a))))
        ring1.append(bm.verts.new(B(x1, y + dy + rr * math.sin(a), z + dz + rr * math.cos(a))))
    made = [bm.faces.new(ring0), bm.faces.new(ring1)]
    for s in range(sides):
        t = (s + 1) % sides
        made.append(bm.faces.new((ring0[s], ring0[t], ring1[t], ring1[s])))
    bmesh.ops.recalc_face_normals(bm, faces=made)


PEEL_SCALE = 1.5   # as medidas das manchas abaixo são multiplicadas por isto


def quad(bm, center, right, up, w, h, normal):
    """Placa (mancha de reboco caído) com UV 0..1; normal para fora da parede."""
    c, r, u = Vector(center), Vector(right), Vector(up)
    w, h = w * PEEL_SCALE, h * PEEL_SCALE
    pts = [c - r * w / 2 - u * h / 2, c + r * w / 2 - u * h / 2, c + r * w / 2 + u * h / 2, c - r * w / 2 + u * h / 2]
    vs = [bm.verts.new(B(*p)) for p in pts]
    f = bm.faces.new(vs)
    f.normal_update()
    if f.normal.dot(B(*normal)) < 0:
        f.normal_flip()
        vs = list(reversed(vs))
    uv = bm.loops.layers.uv.verify()
    corners = {vs[0]: (0, 0), vs[1]: (1, 0), vs[2]: (1, 1), vs[3]: (0, 1)}
    for loop in f.loops:
        loop[uv].uv = corners[loop.vert]


def finish_quads(name, bm):
    """Placas já têm UV própria: só triangula."""
    bmesh.ops.triangulate(bm, faces=list(bm.faces))
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(name, me)
    bpy.context.collection.objects.link(ob)
    return ob


def subtract(target, cutters):
    """Aplica booleanos de diferença e devolve o objeto com a malha resultante."""
    for c in cutters:
        m = target.modifiers.new("corte", 'BOOLEAN')
        m.operation = 'DIFFERENCE'
        try:
            m.solver = 'EXACT'
        except TypeError:
            pass
        m.object = c
    deps = bpy.context.evaluated_depsgraph_get()
    me = bpy.data.meshes.new_from_object(target.evaluated_get(deps))
    old = target.data
    target.modifiers.clear()
    target.data = me
    bpy.data.meshes.remove(old)
    for c in cutters:
        bpy.data.objects.remove(c)
    bm = bmesh.new()
    bm.from_mesh(target.data)
    bmesh.ops.triangulate(bm, faces=list(bm.faces))
    box_uv(bm)
    bm.to_mesh(target.data)
    bm.free()
    return target


def split_inside(ob, inside, inner_name, outer_name):
    """Separa as faces de dentro (centro dentro de inside(x, y, z)) num objeto próprio."""
    def keep(want_inside):
        bm = bmesh.new()
        bm.from_mesh(ob.data)
        dead = []
        for f in bm.faces:
            c = f.calc_center_median()
            ins = inside(c.x, c.z, c.y)
            if ins != want_inside:
                dead.append(f)
        bmesh.ops.delete(bm, geom=dead, context='FACES')
        me = bpy.data.meshes.new(inner_name if want_inside else outer_name)
        bm.to_mesh(me)
        bm.free()
        o = bpy.data.objects.new(me.name, me)
        bpy.context.collection.objects.link(o)
        return o
    a, b = keep(True), keep(False)
    bpy.data.objects.remove(ob)
    return a, b


def arch_cutter(xc, width, y0, spring, z0, z1):
    """Prisma com arco pleno no alto (nicho)."""
    r = width / 2
    pts = [(xc - r, y0), (xc + r, y0)]
    for s in range(1, 12):
        a = math.pi * s / 12
        pts.append((xc + r * math.cos(a), spring + r * math.sin(a)))
    bm = bmesh.new()
    front = [bm.verts.new(B(x, y, z0)) for x, y in pts]
    back = [bm.verts.new(B(x, y, z1)) for x, y in pts]
    bm.faces.new(front)
    bm.faces.new(back)
    n = len(pts)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((front[i], front[j], back[j], back[i]))
    bmesh.ops.recalc_face_normals(bm, faces=list(bm.faces))
    me = bpy.data.meshes.new("cortador")
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new("cortador", me)
    bpy.context.collection.objects.link(ob)
    return ob


def box_object(name, x0, x1, y0, y1, z0, z1):
    bm = bmesh.new()
    box(bm, x0, x1, y0, y1, z0, z1)
    return new_object(name, bm)


# ---------- peças comuns ----------

def shutters(bm_metal, bm_wood, face, center, w, h):
    """Janela fechada: moldura de madeira e duas folhas de metal, na face 'face' (+x, -x, +z ou -z)."""
    cx, cy, cz = center
    t = 0.05
    sgn = 1 if face[0] == '+' else -1
    if face[1] == 'x':
        x = cx + sgn * t / 2
        box(bm_wood, x - t / 2, x + t / 2, cy - h / 2 - 0.08, cy + h / 2 + 0.08, cz - w / 2 - 0.08, cz + w / 2 + 0.08)
        x2 = cx + sgn * (t + 0.02)
        for k in (-1, 1):
            z0 = cz + (0.01 if k > 0 else -w / 2)
            box(bm_metal, x2 - 0.02, x2 + 0.02, cy - h / 2, cy + h / 2, z0, z0 + w / 2 - 0.01)
    else:
        z = cz + sgn * t / 2
        box(bm_wood, cx - w / 2 - 0.08, cx + w / 2 + 0.08, cy - h / 2 - 0.08, cy + h / 2 + 0.08, z - t / 2, z + t / 2)
        z2 = cz + sgn * (t + 0.02)
        for k in (-1, 1):
            x0 = cx + (0.01 if k > 0 else -w / 2)
            box(bm_metal, x0, x0 + w / 2 - 0.01, cy - h / 2, cy + h / 2, z2 - 0.02, z2 + 0.02)


def spout(bm, x, y, z_wall, length, direction):
    """Calha de madeira em U saindo do parapeito (direction +1 = para +z)."""
    z0, z1 = sorted((z_wall - direction * 0.15, z_wall + direction * length))
    w = 0.16
    box(bm, x - w / 2, x + w / 2, y, y + 0.03, z0, z1)
    box(bm, x - w / 2, x - w / 2 + 0.03, y, y + 0.1, z0, z1)
    box(bm, x + w / 2 - 0.03, x + w / 2, y, y + 0.1, z0, z1)


# ---------- casa do altar ----------

def casa_altar():
    x0, x1, z0, z1 = -50.0, -44.0, -32.0, -26.0
    roof0, roof1, parapet = 3.5, 3.8, 4.15

    # Paredes em U, aberta na frente (+z), com pilastras nas pontas da frente (varanda / riwaq).
    poly = [(-50, -32), (-44, -32), (-44, -26.3), (-43.9, -26.3), (-43.9, -25.9), (-44.6, -25.9), (-44.6, -26.3),
            (-44.5, -26.3), (-44.5, -31.5), (-49.5, -31.5), (-49.5, -26.3), (-49.4, -26.3), (-49.4, -25.9),
            (-50.1, -25.9), (-50.1, -26.3), (-50, -26.3)]
    radii = [0.22, 0.22, 0.0, 0.05, 0.05, 0.05, 0.05, 0.0, 0.12, 0.12, 0.0, 0.05, 0.05, 0.05, 0.05, 0.0]
    bm = bmesh.new()
    prism(bm, fillet(poly, radii, 4), -0.05, roof1)
    walls = new_object("CasaAltar_Paredes", bm)
    # Nicho rebaixado atrás do altar: arco de 1,3 m, 0,25 m dentro da parede do fundo.
    walls = subtract(walls, [arch_cutter(-47.0, 1.3, -0.1, 1.3, -31.75, -31.3)])

    def inside(x, y, z):
        return -49.51 < x < -44.49 and -31.76 < z < -26.0 and y < roof0 + 0.01

    split_inside(walls, inside, "CasaAltar_ParedesDentro__RebocoClaro", "CasaAltar_ParedesFora__Reboco")

    # Teto com a claraboia (0,7 x 0,7 m perto do canto de trás, a leste: o sol cai no chão diante do nicho).
    hole = (-45.5, -44.8, -31.35, -30.65)
    roof = box_object("CasaAltar_Teto__Reboco", x0, x1, roof0, roof1, z0, z1)
    subtract(roof, [box_object("c", hole[0], hole[1], roof0 - 0.1, roof1 + 0.1, hole[2], hole[3])])
    bm = bmesh.new()
    ring(bm, x0, x1, z0, z1, 0.25, roof1 - 0.01, parapet, top_jitter=0.03)
    ring(bm, hole[0] - 0.12, hole[1] + 0.12, hole[2] - 0.12, hole[3] + 0.12, 0.12, roof1 - 0.01, roof1 + 0.15)
    new_object("CasaAltar_Parapeito__Reboco", bm)

    # Forro de esteira de junco embaixo do teto.
    ceil = box_object("CasaAltar_Forro__Esteira", -49.5, -44.5, roof0 - 0.012, roof0 - 0.002, -31.5, -26.0)
    subtract(ceil, [box_object("c", hole[0], hole[1], roof0 - 0.1, roof0 + 0.1, hole[2], hole[3])])

    # Vigas de tronco atravessando de leste a oeste, com as pontas para fora; verga da frente; grade da claraboia.
    bm = bmesh.new()
    z = -31.2
    while z < -26.2:
        log_x(bm, x0 - 0.3, x1 + 0.3, roof0 - 0.09, z, 0.09)
        z += 0.6
    log_x(bm, -50.2, -43.8, 3.33, -26.15, 0.14)
    box(bm, hole[0] - 0.05, hole[1] + 0.05, roof1 + 0.15, roof1 + 0.2, -31.03, -30.97)
    box(bm, -45.18, -45.12, roof1 + 0.15, roof1 + 0.2, hole[2] - 0.05, hole[3] + 0.05)
    spout(bm, -45.0, roof1 + 0.05, z1, 0.75, +1)
    shutters_wood = bm
    shutters_metal = bmesh.new()
    shutters(shutters_metal, shutters_wood, '-x', (x0, 1.75, -28.6), 0.8, 0.9)
    new_object("CasaAltar_Madeira__Madeira", shutters_wood)
    new_object("CasaAltar_Janela__Metal", shutters_metal)

    # Esteira enrolada sob a verga da frente.
    bm = bmesh.new()
    log_x(bm, -49.3, -44.7, 3.07, -26.1, 0.1, sides=10)
    new_object("CasaAltar_EsteiraEnrolada__Esteira", bm)

    # Piso de barro alisado.
    box_object("CasaAltar_Piso__Piso", -49.6, -44.4, -0.05, 0.02, -31.6, -26.0)

    # Manchas de reboco caído (placas recortadas), longe da inscrição do Eco na parede do fundo.
    bm = bmesh.new()
    for c, w, h in (((-50.004, 0.65, -30.4), 1.0, 0.75), ((-50.004, 2.6, -27.4), 0.7, 0.5)):
        quad(bm, c, (0, 0, 1), (0, 1, 0), w, h, (-1, 0, 0))
    for c, w, h in (((-48.4, 1.1, -32.004), 1.1, 0.8), ((-45.4, 2.95, -32.004), 0.7, 0.5)):
        quad(bm, c, (1, 0, 0), (0, 1, 0), w, h, (0, 0, -1))
    quad(bm, (-43.996, 0.5, -29.8), (0, 0, 1), (0, 1, 0), 0.9, 0.6, (1, 0, 0))
    quad(bm, (-49.496, 1.9, -27.3), (0, 0, 1), (0, 1, 0), 0.6, 0.45, (1, 0, 0))
    quad(bm, (-44.504, 2.4, -30.3), (0, 0, 1), (0, 1, 0), 0.55, 0.4, (-1, 0, 0))
    finish_quads("CasaAltar_Descascado__Tijolo", bm)


# ---------- casa fechada genérica (vizinha e fundo) ----------

def casa_fechada(name, cx, cz, h, door=None, window=None, beams_axis='x', peel=(), spout_x=None, size=6.0):
    """Casa de barro maciça do blockout (caixa de 6 x 6 m): cantos arredondados, parapeito, vigas e porta turquesa."""
    hs = size / 2
    x0, x1, z0, z1 = cx - hs, cx + hs, cz - hs, cz + hs
    bm = bmesh.new()
    prism(bm, fillet([(x0, z0), (x1, z0), (x1, z1), (x0, z1)], 0.22, 4), -0.05, h)
    ring(bm, x0, x1, z0, z1, 0.25, h - 0.01, h + 0.35, top_jitter=0.03)
    new_object(name + "_Paredes__Reboco", bm)

    wood, metal = bmesh.new(), bmesh.new()
    if beams_axis == 'x':
        z = z0 + 0.6
        while z < z1 - 0.3:
            log_x(wood, x0 - 0.3, x1 + 0.3, h - 0.25, z, 0.09)
            z += 0.9
    if door:
        face, along, w, dh = door      # porta na face 'face', centrada em 'along'
        t = 0.04
        if face == '+x':
            box(wood, x1, x1 + t, 0, dh + 0.1, along - w / 2 - 0.1, along + w / 2 + 0.1)
            box(metal, x1 + t, x1 + t + 0.03, 0, dh, along - w / 2, along + w / 2)
        elif face == '+z':
            box(wood, along - w / 2 - 0.1, along + w / 2 + 0.1, 0, dh + 0.1, z1, z1 + t)
            box(metal, along - w / 2, along + w / 2, 0, dh, z1 + t, z1 + t + 0.03)
    if window:
        face, along, y, w, wh = window
        pos = {'+z': (along, y, z1), '-z': (along, y, z0), '+x': (x1, y, along), '-x': (x0, y, along)}[face]
        shutters(metal, wood, face, pos, w, wh)
    if spout_x is not None:
        spout(wood, spout_x, h + 0.05, z1, 0.7, +1)
    if len(wood.verts):
        new_object(name + "_Madeira__Madeira", wood)
    else:
        wood.free()
    if len(metal.verts):
        new_object(name + "_Metal__Metal", metal)
    else:
        metal.free()

    if peel:
        bm = bmesh.new()
        for face, a, y, w, ph in peel:
            if face == '+z':
                quad(bm, (a, y, z1 + 0.004), (1, 0, 0), (0, 1, 0), w, ph, (0, 0, 1))
            elif face == '-z':
                quad(bm, (a, y, z0 - 0.004), (1, 0, 0), (0, 1, 0), w, ph, (0, 0, -1))
            elif face == '+x':
                quad(bm, (x1 + 0.004, y, a), (0, 0, 1), (0, 1, 0), w, ph, (1, 0, 0))
            else:
                quad(bm, (x0 - 0.004, y, a), (0, 0, 1), (0, 1, 0), w, ph, (-1, 0, 0))
        finish_quads(name + "_Descascado__Tijolo", bm)


def casa_6():
    # Vizinha a leste da casa do altar (caixa de 6 x 6 x 4,4 m em (-40, -28)); porta turquesa a leste.
    casa_fechada("Casa6", -40.0, -28.0, 4.4,
                 door=('+x', -28.0, 1.2, 2.2),
                 window=('+z', -40.0, 1.75, 0.9, 0.9),
                 peel=(('+z', -41.6, 0.7, 1.1, 0.8), ('+z', -38.3, 3.1, 0.8, 0.55), ('-x', -27.0, 1.4, 0.9, 0.7)),
                 spout_x=-38.6)


def fundo():
    # Casas do fundo dos enquadramentos: mesmo kit, sem detalhe de perto. (centro x, z, altura, índice do blockout)
    for i, cx, cz, h in ((1, -70, -40, 3.2), (2, -62, -40, 3.8), (3, -48, -40, 4.4), (4, -40, -38, 3.2), (5, -70, -30, 3.8)):
        casa_fechada(f"Casa{i}", cx, cz, h, door=('+x', cz, 1.2, 2.2), peel=(('+z', cx - 1.2, 1.0, 1.0, 0.7),))


def chao():
    # Terra batida da Aldeia inteira (as emendas com o blockout ficam nas bordas da vila, longe dos quadros).
    box_object("Chao_Aldeia__Chao", -80.0, -32.0, -0.05, 0.012, -50.0, 12.0)


# ---------- exportação ----------

def export(path, names):
    bpy.ops.object.select_all(action='DESELECT')
    for ob in bpy.data.objects:
        if any(ob.name.startswith(n) for n in names):
            ob.select_set(True)
    bpy.ops.export_scene.fbx(filepath=path, use_selection=True, apply_scale_options='FBX_SCALE_UNITS',
                             axis_forward='-Z', axis_up='Y', bake_space_transform=True, mesh_smooth_type='FACE',
                             add_leaf_bones=False, bake_anim=False, object_types={'MESH'})


def main():
    args = sys.argv[sys.argv.index("--") + 1:]
    out_dir, blend = args[0], args[1]
    bpy.ops.wm.read_factory_settings(use_empty=True)
    casa_altar()
    casa_6()
    fundo()
    chao()
    export(out_dir + "/Kit_CasaAltar.fbx", ["CasaAltar_"])
    export(out_dir + "/Kit_Casa6.fbx", ["Casa6_"])
    export(out_dir + "/Kit_CasasFundo.fbx", ["Casa1_", "Casa2_", "Casa3_", "Casa4_", "Casa5_"])
    export(out_dir + "/Kit_ChaoAldeia.fbx", ["Chao_Aldeia"])
    bpy.ops.wm.save_as_mainfile(filepath=blend)
    print("KIT OK", len(bpy.data.objects), "objetos")


main()
