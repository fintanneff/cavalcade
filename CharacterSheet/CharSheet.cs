using Godot;
using System;
using System.Collections.Generic;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Text.Json.Serialization;

public enum ClassKey
{
    Dipshit = 0,
    Pikeman = 1,
    Musketeer = 2,
    Arquebusier = 3,
    Rodelero = 4,
    SwordCuirassier = 5,
    PistolCuirassier = 6,
    FieldCannon = 7,
    Captain = 8,
}
public enum SpeciesKey
{
    Shalaket = 0,
    Valencious = 1,
    Jorgan = 2,
    Fop = 3
}

public struct CharStatTotal
{
    public int Value { get; set; }
    public int IncreaseChance { get; set; }
    public int DecreaseChance { get; set; }
    public void AddStat(Stat stat)
    {
        Value += stat.Value;
        IncreaseChance += stat.IncreaseChance;
        DecreaseChance += stat.DecreaseChance;
    }
}

public class CharSheet
{
    public string Name { get; set; }
    // Character Stats
    public int Level { get; set; }
    public ClassKey ClassKey { get; set; }
    public SpeciesKey SpeciesKey { get; set; }
    public StatSheet CharStats { get; set; }
    // Stat Crunchers
    public CharStatTotal GetBaseStat(BaseStatKey key)
    {
        CharStatTotal toReturn = new CharStatTotal();
        toReturn.Value = 0;
        toReturn.AddStat(CharStats.GetBaseStat(key));
        toReturn.AddStat(StatRegistry.Inst.Classes[ClassKey].ClassStats.GetBaseStat(key));
        toReturn.AddStat(StatRegistry.Inst.Species[SpeciesKey].SpeciesStats.GetBaseStat(key));
        return toReturn;
    }
    public CharStatTotal GetMinorStat(MinorStatKey key)
    {
        CharStatTotal toReturn = new CharStatTotal();
        toReturn.Value = 0;
        toReturn.AddStat(CharStats.GetMinorStat(key));
        toReturn.AddStat(StatRegistry.Inst.Classes[ClassKey].ClassStats.GetMinorStat(key));
        toReturn.AddStat(StatRegistry.Inst.Species[SpeciesKey].SpeciesStats.GetMinorStat(key));
        return toReturn;
    }
    // Current Status
    public int Wounds { get; set; }
    public int Exp { get; set; }
    public int PsycheAttrition { get; set; }
    // Functions to save and load character sheet
    public void SaveTo(string rel_path) { }
    public void LoadFrom(string rel_path) { }
    public static CharSheet FromJSONString(string json_string)
    {
        return JsonSerializer.Deserialize<CharSheet>(json_string, StatRegistry.Inst.jsoptions);
    }
    public string ToJSONString()
    {
        return JsonSerializer.Serialize(this, StatRegistry.Inst.jsoptions);
    }

}