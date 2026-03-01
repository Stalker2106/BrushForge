using Godot;
using Godot.Collections;
using Sledge.Formats.Bsp;
using System.Collections.Generic;

public partial class Entities : GodotObject
{
    const float NORMAL_EPS = 0.99f;
    const float DIST_EPS   = 0.02f;
    
    public static Node3D ParseFuncs(GEntity entity, Node3D mapNode)
    {    
        string modelName = "Model" + entity.Get<string>("model", null).Replace("*", "");
        Node3D entityNode = mapNode.GetNode(modelName) as Node3D;
        if (entity.ClassName == "func_ladder")
        {
            Area3D areaNode = ConvertEntityNodeToArea(mapNode, entityNode, modelName);
            //Hide mesh
            if (false) {
                MeshInstance3D modelMesh = areaNode.GetNodeOrNull("Mesh") as MeshInstance3D;
                if (modelMesh != null) {
                    modelMesh.Visible = false;
                }
            }
            //Attach script
            areaNode.SetScript(GD.Load<Script>("res://entities/Ladder.gd"));
        }
        else if (entity.ClassName == "func_wall")
        {
            //NOTE: for some reason, on living_room, this is true
            //if (entity.Get<int>("rendermode", -1) != -1)
            //{
            //    var collider = entityNode.GetNode("Collider");
            //    collider.QueueFree();
            //}
            //Set alpha
            MeshInstance3D modelMesh = entityNode.GetNode("Mesh") as MeshInstance3D;
            int surfaceCount = modelMesh.GetSurfaceOverrideMaterialCount();
            for (int surface = 0; surface < surfaceCount; surface++)
            {
                BaseMaterial3D originalMat = modelMesh.GetActiveMaterial(surface) as BaseMaterial3D;
                float opacity = entity.Get<int>("renderamt", 255) / 255f;
                originalMat.Transparency = BaseMaterial3D.TransparencyEnum.AlphaDepthPrePass;
                originalMat.AlbedoColor = new Color(1.0f, 1.0f, 1.0f, opacity);
                modelMesh.SetSurfaceOverrideMaterial(surface, originalMat);
            }
        }
        else if (entity.ClassName == "func_water")
        {
            Area3D areaNode = ConvertEntityNodeToArea(mapNode, entityNode, modelName);
            MeshInstance3D modelMesh = areaNode.GetNode("Mesh") as MeshInstance3D;
            //Set Alpha
            int surfaceCount = modelMesh.GetSurfaceOverrideMaterialCount();
            for (int surface = 0; surface < surfaceCount; surface++)
            {
                BaseMaterial3D originalMat = modelMesh.GetActiveMaterial(surface) as BaseMaterial3D;
                float opacity = 0.95f;
                originalMat.Transparency = BaseMaterial3D.TransparencyEnum.AlphaDepthPrePass;
                originalMat.AlbedoColor = new Color(1.0f, 1.0f, 1.0f, opacity);
                originalMat.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
                modelMesh.SetSurfaceOverrideMaterial(surface, originalMat);
            }
            //Attach water script
            areaNode.SetScript(GD.Load<Script>("res://entities/Water.gd"));
            //Bind signals
            areaNode.Call("configure");
        }
        else if (entity.ClassName == "func_illusionary")
        {
            //Delete collider
            var collider = entityNode.GetNode("CollisionShape");
            collider.QueueFree();
            //Set alpha
            MeshInstance3D modelMesh = entityNode.GetNode("Mesh") as MeshInstance3D;
            int surfaceCount = modelMesh.GetSurfaceOverrideMaterialCount();
            for (int surface = 0; surface < surfaceCount; surface++)
            {
                BaseMaterial3D originalMat = modelMesh.GetActiveMaterial(surface) as BaseMaterial3D;
                float opacity = entity.Get<int>("renderamt", 255) / 255f;
                originalMat.Transparency = BaseMaterial3D.TransparencyEnum.AlphaDepthPrePass;
                originalMat.AlbedoColor = new Color(1.0f, 1.0f, 1.0f, opacity);
                modelMesh.SetSurfaceOverrideMaterial(surface, originalMat);
            }
            // Drop all collisions for this brush
            StaticBody3D body = entityNode as StaticBody3D;
            body.CollisionLayer = 0;
            body.CollisionMask = 0;
        }
        else if (entity.ClassName == "func_breakable")
        {
            //Breakables are applied some alpha depending on renderamt
            if (entity.Get<int>("renderamt", -1) != -1)
            {
                MeshInstance3D modelMesh = entityNode.GetNode("Mesh") as MeshInstance3D;
                int surfaceCount = modelMesh.GetSurfaceOverrideMaterialCount();
                for (int surface = 0; surface < surfaceCount; surface++)
                {
                BaseMaterial3D originalMat = modelMesh.GetActiveMaterial(surface) as BaseMaterial3D;
                    float opacity = (entity.Get<int>("renderamt", -1) / 255.0f);
                    originalMat.Transparency = BaseMaterial3D.TransparencyEnum.AlphaDepthPrePass;
                    originalMat.AlbedoColor = new Color(1.0f, 1.0f, 1.0f, opacity);
                    modelMesh.SetSurfaceOverrideMaterial(surface, originalMat);
                }
            }
            //Attach script
            entityNode.SetScript(GD.Load<Script>("res://entities/Breakable.gd"));
            //Bind signals
            entityNode.Call("configure", entity.Get<float>("health", 1.0f));
        }
        else if (entity.ClassName == "func_button") {
            //Attach script
            entityNode.SetScript(GD.Load<Script>("res://entities/Button.gd"));
            //Bind signals
            entityNode.Call("configure", entity.Get<string>("target", null), entity.Get<float>("wait", -1.0f));
        }
        else if (entity.ClassName == "func_rotating") {
            // We place it
            string[] vecs = entity.Get<string>("origin", "0 0 0").Replace(".", ",").Split(" ");
            entityNode.Position = new Vector3(float.Parse(vecs[0]), float.Parse(vecs[1]), float.Parse(vecs[2])).ToSGodotVector3();
            //Attach script
            entityNode.SetScript(GD.Load<Script>("res://entities/Rotating.gd"));
            //Bind signals
            entityNode.Call("configure", entity.Get<float>("speed", 1.0f) / 100.0);
        }
        else if (entity.ClassName == "func_door") {
            float angle = entity.Get<float>("angle", 0f);
            //Attach script
            entityNode.SetScript(GD.Load<Script>("res://entities/Door.gd"));
            //Bind signals
            entityNode.Call("configure", entity.Get<string>("targetname", null),  entity.Get<string>("target", null), entity.Get<float>("speed", 1.0f), angle, entity.Get<float>("lip", 1.0f), entity.Get<float>("wait", -1.0f));
        }
        else if (entity.ClassName == "func_door_rotating") {
            float angle = entity.Get<float>("angle", 0f);
            //Attach script
            entityNode.SetScript(GD.Load<Script>("res://entities/RotatingDoor.gd"));
            //Bind signals
            entityNode.Call("configure", entity.Get<string>("targetname", null),  entity.Get<string>("target", null), entity.Get<float>("speed", 1.0f), angle, entity.Get<float>("lip", 1.0f), entity.Get<int>("wait", -1));
            // We place it
            string[] vecs = entity.Get<string>("origin", "0 0 0").Replace(".", ",").Split(" ");
            entityNode.Position = new Vector3(float.Parse(vecs[0]), float.Parse(vecs[1]), float.Parse(vecs[2])).ToSGodotVector3();
        }
        else if (entity.ClassName == "func_train") {
            //Convert to animatable
            AnimatableBody3D animatableNode = ConvertEntityNodeToAnimatable(mapNode, entityNode, modelName);
            //Attach script
            animatableNode.SetScript(GD.Load<Script>("res://entities/Train.gd"));
            //Bind signals
            animatableNode.Call("configure", entity.Get<string>("target", null), entity.Get<float>("speed", 1.0f));
            entityNode = animatableNode;
        }
        else if (entity.ClassName == "func_tracktrain") {
            //Convert to animatable
            AnimatableBody3D animatableNode = ConvertEntityNodeToAnimatable(mapNode, entityNode, modelName);
            //Attach script
            animatableNode.SetScript(GD.Load<Script>("res://entities/Train.gd"));
            //Bind signals
            animatableNode.Call("configure", entity.Get<string>("target", null), entity.Get<float>("speed", 1.0f));
            entityNode = animatableNode;
        }
        else if (entity.ClassName == "func_friction") {
            // TBImplemented...
        }
        else if (entity.ClassName == "func_pendulum") {
            // TBImplemented...
        }
        else if (entity.ClassName == "func_conveyor") {
            // TBImplemented...
        }
        else if (entity.ClassName == "func_pushable") {
            entityNode = ConvertEntityNodeToRigidBody(mapNode, entityNode, modelName);
        }
        // OpenRomu specific
        else if (entity.ClassName == "func_weather")
        {
            var emitter = GD.Load<PackedScene>("res://entities/Weather/Snow.tscn").Instantiate();
            entityNode.AddChild(emitter);
            //Configure emitter
            emitter.Call("configure");
        }
        else
        {
            entityNode.SetMeta("unsupported", true);
        }
        return entityNode;
    }
    
    public static RigidBody3D ConvertEntityNodeToRigidBody(Node3D mapNode, Node3D entityNode, string modelName)
    {
        //Rename to prevent name clash
        entityNode.Name += "_d";
        //Turn into an body
        RigidBody3D bodyNode = new RigidBody3D();
        bodyNode.Name = modelName;
        bodyNode.CollisionLayer = 2; // 2 are player world colliders
        bodyNode.CollisionMask = 2; // 2 are player world colliders
        mapNode.AddChild(bodyNode);
        //Transfer mesh
        MeshInstance3D modelMesh = entityNode.GetNode("Mesh") as MeshInstance3D;
        if (modelMesh != null) {
            entityNode.RemoveChild(modelMesh);
            bodyNode.AddChild(modelMesh);
            //Create convex collision shape to handle volume collision
            CollisionShape3D collider = new CollisionShape3D();
            collider.Name = "CollisionShape";
            collider.Shape = modelMesh.Mesh.CreateConvexShape();
            bodyNode.AddChild(collider);
        }
        //Free old node, and its nested concave collider
        entityNode.QueueFree();
        return bodyNode;
    }
    
    public static Area3D ConvertEntityNodeToArea(Node3D mapNode, Node3D entityNode, string modelName)
    {
        //Rename to prevent name clash
        entityNode.Name += "_d";
        //Turn into an area
        Area3D areaNode = new Area3D();
        areaNode.Name = modelName;
        areaNode.CollisionLayer = 2; // 2 are player world colliders
        areaNode.CollisionMask = 2; // 2 are player world colliders
        areaNode.Position = entityNode.Position;
        //Transfer mesh
        MeshInstance3D modelMesh = entityNode.GetNodeOrNull("Mesh") as MeshInstance3D;
        if (modelMesh != null) {
            entityNode.RemoveChild(modelMesh);
            areaNode.AddChild(modelMesh);
            //Create convex collision shape to handle volume collision
            CollisionShape3D collider = new CollisionShape3D();
            collider.Name = "CollisionShape";
            collider.Shape = modelMesh.Mesh.CreateConvexShape();
            areaNode.AddChild(collider);
        } else {
            //Transfer shape if any
            CollisionShape3D modelShape = entityNode.GetNodeOrNull("CollisionShape") as CollisionShape3D;
            if (modelShape != null) {
                entityNode.RemoveChild(modelShape);
                areaNode.AddChild(modelShape);
            }
        }
        //Free old node, and its nested concave collider
        entityNode.QueueFree();
        mapNode.AddChild(areaNode);
        return areaNode;
    }
    
    public static AnimatableBody3D ConvertEntityNodeToAnimatable(Node3D mapNode, Node3D entityNode, string modelName)
    {
        //Rename to prevent name clash
        entityNode.Name += "_d";
        //Turn into an animatable
        AnimatableBody3D animatableNode = new AnimatableBody3D();
        animatableNode.Name = modelName;
        animatableNode.CollisionLayer = 1 | 2 | 4;
        animatableNode.CollisionMask = 1 | 2 | 4;
        animatableNode.Position = entityNode.Position;
        //Transfer mesh
        MeshInstance3D modelMesh = entityNode.GetNodeOrNull("Mesh") as MeshInstance3D;
        if (modelMesh != null) {
            entityNode.RemoveChild(modelMesh);
            animatableNode.AddChild(modelMesh);
            //Transfer shape if any
            CollisionShape3D modelShape = entityNode.GetNodeOrNull("CollisionShape") as CollisionShape3D;
            if (modelShape != null) {
                entityNode.RemoveChild(modelShape);
                animatableNode.AddChild(modelShape);
            }
        }
        //Free old node, and its nested concave collider
        entityNode.QueueFree();
        mapNode.AddChild(animatableNode);
        return animatableNode;
    }
}
