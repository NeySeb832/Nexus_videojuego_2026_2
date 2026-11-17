# Convierte un personaje de Mixamo (FBX con texturas 4K incrustadas) a un FBX liviano + texturas
# de máximo 1024 px por canal: Base (RGB + alfa), Normal, MetalSuave (R = metal 0, A = 1 - rugosidad), Emision.
import bpy, sys, os, json
import numpy as np
args = sys.argv[sys.argv.index("--")+1:]
ruta, nombre, salida = args[0], args[1], args[2]
os.makedirs(salida, exist_ok=True)
for f in os.listdir(salida): os.remove(os.path.join(salida, f))
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=ruta)
MAX = 1024

def imagen_de(sock):
    for l in sock.links:
        n = l.from_node
        if n.type == 'TEX_IMAGE': return n.image
        for i in n.inputs:
            if i.is_linked:
                r = imagen_de(i)
                if r: return r
    return None

def pixeles(img, tam):
    img = img.copy()
    if img.size[0] != tam or img.size[1] != tam: img.scale(tam, tam)
    a = np.array(img.pixels[:], dtype=np.float32).reshape(tam, tam, 4)
    bpy.data.images.remove(img)
    return a

def guardar(arr, archivo):
    t = arr.shape[0]
    img = bpy.data.images.new("tmp", t, t, alpha=True)
    img.pixels.foreach_set(arr.ravel())
    img.filepath_raw = os.path.join(salida, archivo)
    img.file_format = 'PNG'
    img.save()
    bpy.data.images.remove(img)
    return archivo

usados = []
for o in bpy.data.objects:
    if o.type == 'MESH':
        for m in o.data.materials:
            if m and m not in usados: usados.append(m)
mapa = {}
for m in usados:
    p = next((n for n in m.node_tree.nodes if n.type == 'BSDF_PRINCIPLED'), None) if m.use_nodes else None
    if not p: continue
    nm = m.name.replace(".", "_")
    info = {}
    base = imagen_de(p.inputs['Base Color'])
    tam = min(MAX, base.size[0]) if base else 512
    if base:
        b = pixeles(base, tam)
        alfa = imagen_de(p.inputs['Alpha'])
        if alfa and alfa != base:
            b[:, :, 3] = pixeles(alfa, tam)[:, :, 0]; info['recorte'] = True
        else: b[:, :, 3] = 1.0
        info['base'] = guardar(b, f"T_{nombre}_{nm}_Base.png")
    nor = imagen_de(p.inputs['Normal'])
    if nor: info['normal'] = guardar(pixeles(nor, min(MAX, nor.size[0])), f"T_{nombre}_{nm}_Normal.png")
    rug = imagen_de(p.inputs['Roughness'])
    ts = min(MAX, rug.size[0]) if rug else 64
    ms = np.zeros((ts, ts, 4), dtype=np.float32)
    ms[:, :, 3] = 1.0 - (pixeles(rug, ts)[:, :, 0] if rug else 0.75)
    info['metalSuave'] = guardar(ms, f"T_{nombre}_{nm}_MetalSuave.png")
    em = imagen_de(p.inputs['Emission Color'])
    if em: info['emision'] = guardar(pixeles(em, min(MAX, em.size[0])), f"T_{nombre}_{nm}_Emision.png")
    mapa[m.name] = info

json.dump(mapa, open(os.path.join(salida, f"{nombre}_materiales.json"), "w"), indent=1)
bpy.ops.object.select_all(action='DESELECT')
for o in bpy.data.objects:
    if o.type in ('ARMATURE', 'MESH'): o.select_set(True)
bpy.ops.export_scene.fbx(filepath=os.path.join(salida, nombre + ".fbx"), use_selection=True,
    object_types={'ARMATURE', 'MESH'}, add_leaf_bones=False, bake_anim=False, path_mode='STRIP',
    embed_textures=False, mesh_smooth_type='FACE')
print("LISTO", nombre, json.dumps(mapa))
