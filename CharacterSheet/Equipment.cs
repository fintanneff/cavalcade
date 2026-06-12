using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json;

public struct EquipmentAction
{
    public string Name { get; set; }
    public bool RequiresLoading { get; set; }
    public int MaxRange { get; set; }
    public int MinRange { get; set; }
    public Vector2I[] SpecialPattern { get; set; }
    public int BaseDamage { get; set; }
    public MinorStatKey[] ScalingStats { get; set; }
    public double BaseDurabilityLoss { get; set; }
    public int BasePriority { get; set; }
}

public class Equipment
{
    public string Name { get; set; }
    public WeaponType Type { get; set; }
    public bool MustBePrimary { get; set; }
    public double MaxDurability { get; set; }
    public double Weight { get; set; }
    public EquipmentAction[] Actions { get; set; }

    public static Equipment FromJSONString(string json_string)
    {
        return JsonSerializer.Deserialize<Equipment>(json_string, StatRegistry.Inst.jsoptions);
    }
    public string ToJSONString()
    {
        return JsonSerializer.Serialize(this, StatRegistry.Inst.jsoptions);
    }

    public Equipment Clone()
    {
        Equipment eq = new Equipment()
        {
            Name = Name,
            Type = Type,
            MaxDurability = MaxDurability,
            Actions = (EquipmentAction[])Actions.Clone(),
        };
        return eq;
    }

}
