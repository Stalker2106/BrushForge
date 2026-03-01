extends Control

const FaceSelectedMaterial = preload("res://materials/FaceSelectedMaterial.tres");
const NoTexture = preload("res://sprites/Missing.png");

var texturePreview;
var identifierLabel;
var dataLabel;

var overlayMesh;

# Called when the node enters the scene tree for the first time.
func _ready():
    overlayMesh = MeshInstance3D.new();
    var world = get_node("/root/App/Layout/CenterLayout/Main/Views/3DView/World");
    world.add_child(overlayMesh);
    texturePreview = get_node("ScrollContainer/Layout/TexturePreview");
    identifierLabel = get_node("ScrollContainer/Layout/IdentifierLabel");
    dataLabel = get_node("ScrollContainer/Layout/DataLabel");

# Called every frame. 'delta' is the elapsed time since the previous frame.
func _input(event):
    if event is InputEventMouseButton && event.button_index == MOUSE_BUTTON_LEFT && event.pressed:
        inspect();

func inspect():
    var collision = get_node("/root/App").view3D.camera.getRaycastHit();
    if !collision:
        return;
    var collider = collision.collider;
    if collider is Entity:
        identifierLabel.set_text(collider.identifier);
        texturePreview.set_texture(NoTexture);
        var dataText = "";
        var fields = collider.getFields();
        for field in fields.keys():
            dataText += "%s: %s\n" % [field, fields[field]];
        dataLabel.set_text(dataText);
    elif collider is StaticBody3D:
        inspectMesh(collision, collider);
    else:
        identifierLabel.set_text("?");
        dataLabel.set_text("...");

func inspectMesh(collision, collider):
    var mesh_inst = collider.get_node_or_null("Mesh")
    if mesh_inst == null:
        return

    var mesh = mesh_inst.mesh
    if mesh == null:
        return

    var mdt = MeshDataTool.new()
    var hit_surface := -1

    # --- Find which surface contains the hit triangle ---
    for surface in range(mesh.get_surface_count()):
        mdt.create_from_surface(mesh, surface)

        for face in range(mdt.get_face_count()):
            var v0 = mdt.get_vertex(mdt.get_face_vertex(face, 0))
            var v1 = mdt.get_vertex(mdt.get_face_vertex(face, 1))
            var v2 = mdt.get_vertex(mdt.get_face_vertex(face, 2))

            if Geometry3D.segment_intersects_triangle(
                collision.position - collision.normal,
                collision.position + collision.normal,
                v0, v1, v2
            ):
                hit_surface = surface
                break

        if hit_surface != -1:
            break

    if hit_surface == -1:
        return

    # --- Rebuild highlight mesh from that surface only ---
    mdt.clear()
    mdt.create_from_surface(mesh, hit_surface)

    var highlight_mesh = ImmediateMesh.new()
    highlight_mesh.surface_begin(Mesh.PRIMITIVE_TRIANGLES, FaceSelectedMaterial)

    var normal_offset = collision.normal * 0.1

    for face in range(mdt.get_face_count()):
        var v0 = mdt.get_vertex(mdt.get_face_vertex(face, 0))
        var v1 = mdt.get_vertex(mdt.get_face_vertex(face, 1))
        var v2 = mdt.get_vertex(mdt.get_face_vertex(face, 2))

        highlight_mesh.surface_add_vertex(v0 + normal_offset)
        highlight_mesh.surface_add_vertex(v1 + normal_offset)
        highlight_mesh.surface_add_vertex(v2 + normal_offset)

    highlight_mesh.surface_end()
    overlayMesh.mesh = highlight_mesh

    # --- UI Update ---
    var material = mesh.surface_get_material(hit_surface)
    if material and material is StandardMaterial3D:
        texturePreview.texture = material.albedo_texture

    identifierLabel.text = "Surface: %d" % hit_surface
