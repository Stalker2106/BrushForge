using Godot;
using Godot.Collections;

public partial class Entities : GodotObject
{
    public static Node3D ParseAmbient(GEntity entity)
    {
        Node3D entityNode = null;
        if (entity.ClassName == "ambient_generic")
        {
            AudioStreamPlayer3D speaker = new AudioStreamPlayer3D();
            entityNode = speaker;
        }
        return entityNode;
    }
}
