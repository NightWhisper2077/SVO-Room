import math
from pathlib import Path

import bpy


ROOT = Path(__file__).resolve().parents[1]
MODEL_DIR = ROOT / "Models"
MODEL_DIR.mkdir(parents=True, exist_ok=True)


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def make_material(name, color, metallic=0.0, roughness=0.55):
    material = bpy.data.materials.new(name)
    material.use_nodes = True
    bsdf = material.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = color
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    return material


def assign(obj, material):
    obj.data.materials.append(material)
    return obj


def add_uv_sphere(name, location, scale, material, segments=32, rings=16):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, location=location)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    assign(obj, material)
    return obj


def add_cylinder(name, location, radius, depth, material, vertices=32, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    assign(obj, material)
    return obj


def add_cube(name, location, scale, material, rotation=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(location=location, rotation=rotation)
    obj = bpy.context.object
    obj.name = name
    obj.scale = scale
    assign(obj, material)
    return obj


def build_drone():
    body = make_material("warm white shell", (0.85, 0.88, 0.82, 1), 0.05)
    dark = make_material("matte graphite", (0.04, 0.05, 0.055, 1), 0.1)
    blue = make_material("soft blue lens", (0.05, 0.55, 1.0, 1), 0.0, 0.2)
    orange = make_material("safety orange accents", (1.0, 0.42, 0.08, 1), 0.0, 0.45)

    add_uv_sphere("rounded main body", (0, 0, 0), (0.42, 0.22, 0.32), body)
    add_uv_sphere("front camera lens", (0, -0.235, 0.03), (0.11, 0.035, 0.11), blue, 24, 12)
    add_cube("top status strip", (0, -0.02, 0.22), (0.28, 0.035, 0.025), orange)

    arm_positions = [(-0.52, -0.28, 0), (0.52, -0.28, 0), (-0.52, 0.28, 0), (0.52, 0.28, 0)]
    for index, (x, y, z) in enumerate(arm_positions, start=1):
        angle = math.atan2(y, x)
        add_cube(f"carbon arm {index}", (x * 0.5, y * 0.5, 0), (0.36, 0.035, 0.025), dark, (0, 0, angle))
        add_cylinder(f"rotor ring {index}", (x, y, z), 0.16, 0.035, dark, 40)
        add_cylinder(f"rotor hub {index}", (x, y, z + 0.012), 0.045, 0.055, body, 24)
        add_cube(f"rotor blade a {index}", (x, y, z + 0.045), (0.14, 0.018, 0.008), orange)
        add_cube(f"rotor blade b {index}", (x, y, z + 0.045), (0.018, 0.14, 0.008), orange)

    bpy.ops.object.empty_add(type="PLAIN_AXES", location=(0, 0, 0))
    root = bpy.context.object
    root.name = "SafetyGuideDrone_Root"

    for obj in bpy.context.scene.objects:
        if obj != root:
            obj.parent = root


def save_files():
    blend_path = MODEL_DIR / "SafetyGuideDrone.blend"
    fbx_path = MODEL_DIR / "SafetyGuideDrone.fbx"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend_path))
    bpy.ops.export_scene.fbx(
        filepath=str(fbx_path),
        use_selection=False,
        apply_scale_options="FBX_SCALE_ALL",
        object_types={"MESH", "EMPTY"},
        add_leaf_bones=False,
    )


reset_scene()
build_drone()
save_files()
