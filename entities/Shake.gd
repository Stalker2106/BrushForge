extends Node

var amplitude;
var radius;
var duration;
var frequency;

func configure(amplitude_ : float, radius_ : float, duration_ : float, frequency_ : float):
    amplitude = amplitude_;
    radius = radius_;
    duration = duration_;
    frequency = frequency_;

func trigger(sender: Node):
    var localPlayer = SystemManager.game.getPlayer(multiplayer.get_unique_id());
    localPlayer.node.fpsCamera.shake(amplitude, duration, frequency);
