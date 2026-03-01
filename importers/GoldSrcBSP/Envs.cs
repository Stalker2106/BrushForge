using Godot;
using Godot.Collections;

public partial class Entities : GodotObject
{
    public static Node3D ParseEnvs(GEntity entity, Node3D mapNode)
    {
        Node3D entityNode = null;
        if (entity.ClassName == "env_shake") {
            entityNode = new Node3D();
            entityNode.SetScript(GD.Load<Script>("res://entities/Shake.gd"));
            entityNode.Call("configure", entity.Get<float>("amplitude", 0.0f),  entity.Get<float>("radius", 0.0f),  entity.Get<float>("duration", 0.0f),  entity.Get<float>("frequency", 0.0f));
            //entityNode.Call("configure", );
        }
        else if (entity.ClassName == "env_fade") {
            entityNode = new Node3D();
            entityNode.SetScript(GD.Load<Script>("res://entities/Fade.gd"));
            //entityNode.Call("configure", );
        }
        else if (entity.ClassName == "env_sprite") {
            entityNode = new Sprite3D();
            //entityNode.Billboard = BaseMaterial3D.BillboardMode.Enabled;
        }
        else if (entity.ClassName == "env_beam") {
            Sprite3D sprite = new Sprite3D();
            string textureName = entity.Get<string>("texture", null);
            Texture2D texture = mapNode.Call("getTexture", textureName).As<Texture2D>();
            sprite.Texture = texture;
            // Compute size
            var startTargetName = entity.Get<string>("LightningStart", null);
            var startEntities = mapNode.Call("getTargets", startTargetName).As<Array<Node3D>>();
            if (startEntities.Count > 0) {
                var endTargetName = entity.Get<string>("LightningEnd", null);
                var endEntities = mapNode.Call("getTargets", endTargetName).As<Array<Node3D>>();
                if (endEntities.Count > 0) {
                    // Set region
                    sprite.RegionEnabled = true;
                    var length = (startEntities[0].Position - endEntities[0].Position).Length();
                    sprite.RegionRect = new Rect2(0,0, length, texture.GetSize().Y);
                }
            }
        }
        else if (entity.ClassName == "env_shooter") {
            
        }
        else if (entity.ClassName == "env_message") {
            
        }
        return entityNode;
    }
}
