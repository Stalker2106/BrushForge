extends Node3D
class_name Trigger

var target;
var auto : bool;
var multiple : bool;
var triggered : bool;

func _init() -> void:
    auto = false;
    multiple = true;

func bind(multiple_: bool, target_ : String, auto_: bool):
    multiple = multiple_;
    target = target_;
    auto = auto_;

func _ready() -> void:
    if auto:
        trigger(self);

func bindSignals():
    connect("body_entered", Callable(self, "body_entered"));

func body_entered(body: Node):
    if body is not Player:
        return; # Not a player
    trigger(self);

func trigger(sender : Node) -> void:
    triggered = true;
    if !get_node("/root/App").view3D.currentLevelNode.targets.has(target):
        return; # No connected targets
    var connectedTargets = get_node("/root/App").view3D.currentLevelNode.targets[target];
    if connectedTargets:
        for tgt in connectedTargets:
            if !tgt || tgt == self || tgt == sender || !tgt.has_method("trigger"):
                continue; # Skip destroyed, self & sender
            tgt.trigger(sender);
