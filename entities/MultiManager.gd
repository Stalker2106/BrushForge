extends Trigger

var targets : Dictionary;

func _ready() -> void:
    for targetName in targets:
        var time = float(targets[targetName]);
        var timer = Timer.new();
        add_child(timer);
        timer.one_shot = true;
        timer.connect("timeout", func ():
            var targetNodes = get_node("/root/App").view3D.currentLevelNode.getTargets(targetName);
            if targetNodes.size() > 0 && targetNodes[0]:
                if !targetNodes[0].has_method("trigger"):
                    targetNodes[0].visible = !visible;
                else:
                    targetNodes[0].trigger(self);
        )
        timer.start(time);
    addDebug();

func configure(targets_ : Dictionary):
    targets = targets_;

func addDebug():
    if targets.size() <= 0:
        return; # No link
    var link = ImmediateMesh.new();
    link.surface_begin(Mesh.PRIMITIVE_LINES);
    for targetName in targets:
        var targetNodes = get_node("/root/App").view3D.currentLevelNode.getTargets(targetName);
        if targetNodes.size() > 0 && targetNodes[0]:
            link.surface_add_vertex(to_local(global_position));
            link.surface_add_vertex(to_local(targetNodes[0].global_position));
    link.surface_end();
    var debugLink = MeshInstance3D.new();
    debugLink.mesh = link;
    add_child(debugLink);
    
