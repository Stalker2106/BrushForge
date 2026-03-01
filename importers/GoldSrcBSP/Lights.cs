using Godot;
using Godot.Collections;
using Sledge.Formats.Bsp;

public partial class Entities : GodotObject
{
    public static Node3D ParseLights(GEntity entity)
    {
        Node3D entityNode = null;
        // Basic light
        if (entity.ClassName == "light")
        {
            OmniLight3D light = new OmniLight3D();
            string[] color = entity.Get<string>("_light", null).Trim().Split(" ");
            if (color.Length >= 4)
            {
                light.LightColor = new Color(int.Parse(color[0]) / 255.0f, int.Parse(color[1]) / 255.0f, int.Parse(color[2]) / 255.0f);
                entityNode = light;
                //Set layer for viewmodel
                light.SetLayerMaskValue(2, true);
            }
        }
        // Light Spot
        else if (entity.ClassName == "light_spot")
        {
            SpotLight3D light = new SpotLight3D();
            string[] color = entity.Get<string>("_light", null).Trim().Split(" ");
            if (color.Length >= 4)
            {
                light.LightColor = new Color(int.Parse(color[0]) / 255.0f, int.Parse(color[1]) / 255.0f, int.Parse(color[2]) / 255.0f);
                entityNode = light;
                //Set layer for viewmodel
                light.SetLayerMaskValue(2, true);
            }
        }
        // Env light (Sun)
        else if (entity.ClassName == "light_environment")
        {
            DirectionalLight3D light = new DirectionalLight3D();
            light.ShadowEnabled = true;
            string[] color = entity.Get<string>("_light", null).Trim().Split(" ");
            if (color.Length >= 4)
            {
                light.LightColor = new Color(int.Parse(color[0]) / 255.0f, int.Parse(color[1]) / 255.0f, int.Parse(color[2]) / 255.0f);
                light.Rotation = new Vector3(Mathf.DegToRad(entity.Get<float>("pitch", -45.0f)), -Mathf.DegToRad(entity.Get<float>("angle", 0f)), 0);
                entityNode = light;
                //Set layer for viewmodel
                light.SetLayerMaskValue(2, true);
            }
        }
        return entityNode;
    }
}
