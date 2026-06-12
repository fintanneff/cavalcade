using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

public enum WeaponType
{
    Sword,
    Pike,
    Musket,
    Pistol,
    Cannon,
    Staff
}

public class ClassSheet
{
    public string Name { get; set; }
    public StatSheet ClassStats { get; set; }
    public WeaponType primaryWeaponType { get; set; }
    public WeaponType[] secondaryWeaponTypes { get; set; }
    public static ClassSheet FromJSONString(string json_string)
    {
        return JsonSerializer.Deserialize<ClassSheet>(json_string,StatRegistry.Inst.jsoptions);
    }
    public string ToJSONString()
    {
        return JsonSerializer.Serialize(this, StatRegistry.Inst.jsoptions);
    }

    [JsonIgnore] public PackedScene DefaultPawnScene;
}
