extends Node3D

var speed;

func configure(speed_ : float):
    speed = speed_;
    
func _process(delta: float) -> void:
    rotation.y += speed * delta;
