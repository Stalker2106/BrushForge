using Godot;
using Godot.Collections;

public partial class Entities : GodotObject
{
    public static Node3D ParseInfos(GEntity entity, Node3D mapNode)
    {
        Node3D entityNode = null;
        // Decals
        if (entity.ClassName == "infodecal")
        {
            var textureName = entity.Get<string>("texture", "").ToLower();
            Texture2D texture = mapNode.Call("getTexture", textureName).As<Texture2D>();
            if (texture != null)
            {
                Decal decal = new Decal();
                decal.TextureAlbedo = texture;
                entityNode = decal;
            }
            else
            {
                GD.Print("Couldn't find decal texture "+textureName);
            }
        }
        else if (entity.ClassName == "info_node")
        {
            Node3D node = new Node3D();
            entityNode = node;
        }
        else if (entity.ClassName == "info_target")
        {
            Node3D node = new Node3D();
            entityNode = node;
        }
        else if (entity.ClassName == "info_landmark")
        {
            Node3D node = new Node3D();
            entityNode = node;
        }
        else if (entity.ClassName == "info_teleport_destination")
        {
            Node3D node = new Node3D();
            entityNode = node;
        }
        else if (entity.ClassName == "spawn_deathmatch" || entity.ClassName == "info_player_start" || entity.ClassName == "info_player_deathmatch")
        {
            Node3D spawn = new Node3D();
            entityNode = spawn;
        }
        return entityNode;
    }
}
