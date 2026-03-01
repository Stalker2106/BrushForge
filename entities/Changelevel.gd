extends Area3D

var map : String;

func _ready() -> void:
    connect("body_entered", Callable(self, "changelevel"));

func configure(map_: String):
    map = map_;

func changelevel(body: Node):
    pass;
