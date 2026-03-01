extends Trigger
class_name Track

var nextName;
var next : Node3D;

var speed : float;

var debugLink;

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
    if nextName:
        var nextNodes = get_node("/root/App").view3D.currentLevelNode.getTargets(nextName);
        if nextNodes.size() > 0:
            next = nextNodes[0];
    addDebug();

func configure(targetName_ : String, nextName_: String):
    super.bind(false, targetName_, false);
    nextName = nextName_;

func addDebug():
    if next == null:
        return; # No link
    var link = ImmediateMesh.new();
    link.surface_begin(Mesh.PRIMITIVE_LINES)
    link.surface_add_vertex(to_local(global_position));
    link.surface_add_vertex(to_local(next.global_position));
    link.surface_end();
    debugLink = MeshInstance3D.new();
    debugLink.mesh = link;
    add_child(debugLink);
    
func removeDebug():
    debugLink.queue_free();
