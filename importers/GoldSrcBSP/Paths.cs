using Godot;
using Godot.Collections;

public partial class Entities : GodotObject
{
    public static Node3D ParsePaths(GEntity entity, Node3D mapNode)
    {
        Node3D entityNode = null;
        if (entity.ClassName == "path_track") {
            entityNode = new Node3D();
            entityNode.SetScript(GD.Load<Script>("res://entities/Track.gd"));
            var targetName = entity.Get<string>("message", null);
            var nextName = entity.Get<string>("target", null);
            entityNode.Call("configure", targetName, nextName);
        }
        else if (entity.ClassName == "path_corner") {
            entityNode = new Node3D();
            entityNode.SetScript(GD.Load<Script>("res://entities/Track.gd"));
            var targetName = entity.Get<string>("message", null);
            var nextName = entity.Get<string>("target", null);
            entityNode.Call("configure", targetName, nextName);
        }
        return entityNode;
    }
}
