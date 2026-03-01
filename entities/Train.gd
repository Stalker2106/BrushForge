extends AnimatableBody3D

var target;

var currentTrack;

var active;
var speed;

func _ready() -> void:
    active = false;
    sync_to_physics = false;
    if target != null:
        var targetNodes = get_node("/root/App").view3D.currentLevelNode.getTargets(target);
        if targetNodes.size() > 0:
            currentTrack = targetNodes[0];
            position = currentTrack.position;
            rotation = currentTrack.rotation;

func configure(target_: String, speed_: float):
    speed = speed_ * 0.05;
    target = target_;

func trigger(sender : Node):
    print("%s triggered by %s" % [name, sender.name])
    active = true;
    
func _physics_process(delta: float) -> void:
    if !active || currentTrack == null:
        return;
    position = position.move_toward(currentTrack.global_position, speed * delta);
    if position.is_equal_approx(currentTrack.global_position):
        currentTrack.trigger(self);
        if currentTrack.next != null:
            currentTrack = currentTrack.next;
            var direction = (currentTrack.position - global_position).normalized()
            # Assign direction to train
            var look_basis = Basis.looking_at(-direction, Vector3.UP);
            rotation = look_basis.get_euler();
        else:
            active = false;
