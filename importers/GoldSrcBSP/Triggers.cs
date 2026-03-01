using Godot;
using Godot.Collections;

public partial class Entities : GodotObject
{
    public static Node3D ParseBrushTriggers(GEntity entity, Node3D mapNode)
    {
        string modelName = "Model" + entity.Get<string>("model", null).Replace("*", "");
        Node3D entityNode = mapNode.GetNodeOrNull(modelName) as Node3D;
        if (entity.ClassName == "trigger_changelevel") {
            Area3D areaNode = ConvertEntityNodeToArea(mapNode, entityNode, modelName);
            //Hide mesh
            if (false) {
                MeshInstance3D modelMesh = areaNode.GetNodeOrNull("Mesh") as MeshInstance3D;
                if (modelMesh != null)
                    modelMesh.Visible = false;
            }
            //Attach script
            areaNode.SetScript(GD.Load<Script>("res://entities/Changelevel.gd"));
            areaNode.Call("configure", entity.Get<string>("map", ""));
        }
        else if (entity.ClassName == "trigger_multiple" || entity.ClassName == "trigger_once") {
            Area3D areaNode = ConvertEntityNodeToArea(mapNode, entityNode, modelName);
            //Hide mesh
            if (false) {
                MeshInstance3D modelMesh = areaNode.GetNodeOrNull("Mesh") as MeshInstance3D;
                if (modelMesh != null)
                    modelMesh.Visible = false;
            }
            //Attach script
            areaNode.SetScript(GD.Load<Script>("res://entities/Trigger.gd"));
            areaNode.Call("bind", entity.ClassName == "trigger_multiple" ? true : false, entity.Get<string>("target", null), false);
            areaNode.Call("bindSignals");
        }
        else if (entity.ClassName == "trigger_hurt") {
            Area3D areaNode = ConvertEntityNodeToArea(mapNode, entityNode, modelName);
            //Hide mesh
            if (false) {
                MeshInstance3D modelMesh = areaNode.GetNodeOrNull("Mesh") as MeshInstance3D;
                if (modelMesh != null)
                    modelMesh.Visible = false;
            }
            //Attach script
            areaNode.SetScript(GD.Load<Script>("res://entities/Hurt.gd"));
            areaNode.Call("configure", entity.Get<int>("dmg", 1));
        } 
        else if (entity.ClassName == "trigger_autosave") {
            // Drop collider
            var collider = entityNode.GetNode("CollisionShape");
            if (collider != null)
                collider.QueueFree();
            //Hide mesh
            if (false) {
                MeshInstance3D modelMesh = entityNode.GetNodeOrNull("Mesh") as MeshInstance3D;
                if (modelMesh != null)
                    modelMesh.Visible = false;
            }
        }
        else if (entity.ClassName == "trigger_auto") {
            
        }
        else {
            entityNode.SetMeta("unsupported", true);
        }
        return entityNode;
    }
    
    public static Node3D ParsePointTriggers(GEntity entity, Node3D mapNode)
    {
        Node3D entityNode = null;
        if (entity.ClassName.StartsWith("trigger_auto"))
        {
            entityNode = new Node3D();
            entityNode.SetScript(GD.Load<Script>("res://entities/Trigger.gd"));
            var target = entity.Get<string>("target", null);
            entityNode.Call("bind", false, target, true);
        }
        return entityNode;
    }
}
