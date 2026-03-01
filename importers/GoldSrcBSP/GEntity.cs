using Godot;
using Godot.Collections;
using Sledge.Formats.Bsp.Objects;
using System;

public partial class GEntity : GodotObject
{
    public Entity bspEntity;

    public GEntity(Entity entity)
    {
        bspEntity = entity;
    }

    public Dictionary<string, Variant> GetFields()
    {
        Dictionary<string, Variant> dict = new Dictionary<string, Variant>();
        foreach (System.Collections.Generic.KeyValuePair<string, string> entry in bspEntity.KeyValues)
            dict[entry.Key] = entry.Value;
        return dict;
    }

    public string GetName()
    {
        return Get<string>("targetname", "");
    }

    //Passthrough
    public string ClassName { get { return bspEntity.ClassName; } set { bspEntity.ClassName = value; } }
    public T Get<T>(string key, T defaultValue)
    {
        return bspEntity.Get<T>(key, defaultValue);
    }
}
