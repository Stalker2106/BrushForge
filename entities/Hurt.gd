extends Area3D

var damage;

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
    connect("body_entered", Callable(self, "bodyEntered"));

func configure(damage_):
    damage = damage_;

func bodyEntered(body : Node3D):
    if body is Player:
        body.hurt(-1, -1, damage, false);
    
