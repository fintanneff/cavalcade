using Godot;
using System;
using System.Text.Json;

public class SpeciesSheet
{
    public string Name { get; set; }
    public StatSheet SpeciesStats { get; set; }
    public static SpeciesSheet FromJSONString(string json_string)
    {
        return JsonSerializer.Deserialize<SpeciesSheet>(json_string, StatRegistry.Inst.jsoptions);
    }
    public string ToJSONString()
    {
        return JsonSerializer.Serialize(this, StatRegistry.Inst.jsoptions);
    }
}
