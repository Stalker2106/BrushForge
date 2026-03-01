extends Trigger
class_name Door

enum DoorState {
    Open,
    Closed
}

var direction;
var speed;
var wait : float;
var active : bool;
var state : DoorState;
var openPos;
var closedPos;

var targetname;
var lip;

var wait_timer;

func _init() -> void:
    active = false;
    state = DoorState.Closed;
    speed = 0;
    wait = -1.0;
    wait_timer = 0.0;
    openPos = position;
    closedPos = position;
    direction = Vector3.ZERO;

func _ready() -> void:
    var aabb = get_node("Mesh").global_transform * get_node("Mesh").mesh.get_aabb()
    openPos = position + direction * (aabb.size.x + lip);
    if targetname == null:
        addTriggerArea(aabb);

func configure(targetName_ : String, target_ : String, speed_ : float, angle : float, lip_ : float, wait_ : int):
    if target_:
        bind(true, target_, false);
    speed = speed_ / 100.0;
    wait = wait_;
    if angle == -1.0:
        direction = Vector3.UP;
    else:
        var rad = deg_to_rad(angle);
        direction = Vector3.FORWARD.rotated(Vector3.UP, rad).normalized()
    targetname = targetName_;
    lip = lip_ / 32.0;

func addTriggerArea(aabb : AABB):
    var area = Area3D.new();
    area.position = aabb.get_center();
    area.set_collision_layer_value(1, false);
    area.set_collision_mask_value(1, false);
    area.set_collision_layer_value(2, true);
    area.set_collision_mask_value(2, true);
    var shape = CollisionShape3D.new();
    var box = BoxShape3D.new()
    box.size = aabb.size + Vector3(0.1, 0.1, 0.1);
    shape.shape = box
    area.add_child(shape);
    add_child(area);
    area.connect("body_entered", Callable(self, "body_entered"));

func body_entered(body):
    if body is not Player:
        return;
    trigger(self);

func trigger(sender : Node):
    super.trigger(sender);
    if state == DoorState.Open:
        return; #Not player or Already opened
    active = true;

func _physics_process(delta: float) -> void:
    if !active:
        if state == DoorState.Open && (wait > 0.0):
            wait_timer += delta;
            if wait_timer > wait:
                wait_timer = 0;
                active = true;
        return; # Not active;
    position += direction * (1 if state == DoorState.Closed else -1) * speed * delta;
    if state == DoorState.Open && position.distance_to(closedPos) <= 0.01:
        active = false;
        state = DoorState.Closed;
    elif state == DoorState.Closed && position.distance_to(openPos) <= 0.01:
        active = false;
        state = DoorState.Open;
        
