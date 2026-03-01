extends Node

var surfaceY : float;
var playerInside : Node;

func configure() -> void:
    playerInside = null;
    connect("body_shape_entered", Callable(self, "bodyShape").bind(true));
    connect("body_shape_exited", Callable(self, "bodyShape").bind(false));
    #We grab surfaceY from debug_shape as a "cheap" way to get mesh position / size.
    var aabb = get_node("CollisionShape").shape.get_debug_mesh().get_aabb();
    surfaceY = aabb.position.y + (aabb.end - aabb.position).y;

func bodyShape(_bodyRID : RID, body : PhysicsBody3D, bodyShapeIndex : int, _localShapeIndex : int, enabled : bool) -> void:
    if body is LocalPlayer && bodyShapeIndex == 0:
        body.swimming = enabled;
        if enabled:
            playerInside = body;
        else:
            playerInside = null;
            setWaterMode(false);

func _physics_process(_delta) -> void:
    if !playerInside:
        return; #Nothing to do
    if playerInside.look.get_global_position().y <= surfaceY:
        setWaterMode(true);
    else:
        setWaterMode(false);

func setWaterMode(enabled):
    SystemManager.hud.setWaterOverlay(enabled);
