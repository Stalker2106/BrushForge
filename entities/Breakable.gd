extends Node
class_name Breakable

var health;

# Called when the node enters the scene tree for the first time.
func _ready() -> void:
    pass # Replace with function body.

func configure(health_: int):
    health = health_;

func hurt(damage: int) -> void:
    health -= damage;
    if health <= 0:
        trigger(self);

#Trigger will just destroy it
func trigger(_sender: Node):
    queue_free();
