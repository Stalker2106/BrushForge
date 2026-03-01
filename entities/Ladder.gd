extends Area3D
class_name Ladder

var up_vector: Vector3 = Vector3.UP;

func _ready() -> void:
    connect("body_entered", Callable(self, "setClimbing").bind(true));
    connect("body_exited", Callable(self, "setClimbing").bind(false));

func setClimbing(body: Node3D, climb: bool):
    if body is not Player:
        return;
    body.setClimbing(self if climb else null);
    
func getLadderNormal(player: Node3D) -> Vector3:
    var mesh_instance := get_node("Mesh");
    if !mesh_instance:
        return Vector3.ZERO;

    var player_pos: Vector3 = player.global_position
    var best_normal: Vector3 = Vector3.ZERO
    var best_distance: float = INF

    # Only works if mesh is ArrayMesh
    for surface_idx in mesh_instance.mesh.get_surface_count():
        var arrays = mesh_instance.mesh.surface_get_arrays(surface_idx)
        var vertices: PackedVector3Array = arrays[Mesh.ARRAY_VERTEX]

        # iterate triangles (assumes surface uses triangles)
        for i in range(0, vertices.size(), 3):
            if i + 2 >= vertices.size():
                continue
            var a = mesh_instance.to_global(vertices[i])
            var b = mesh_instance.to_global(vertices[i+1])
            var c = mesh_instance.to_global(vertices[i+2])

            var normal = (b - a).cross(c - a).normalized()

            # ensure normal points toward player
            var to_player = (player_pos - a).normalized()
            if normal.dot(to_player) < 0:
                normal = -normal

            var dist = abs((player_pos - a).dot(normal))
            if dist < best_distance:
                best_distance = dist
                best_normal = normal
    return best_normal
