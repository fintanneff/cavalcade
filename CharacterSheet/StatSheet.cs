using Godot;
using System;
using System.Text.Json.Nodes;
using System.Text.Json;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public enum BaseStatKey
{
    HP                  = 0,
    Stamina             = 1,
    Priority            = 2,
    Accuracy            = 3,
    Evasion             = 4,
    Defense             = 5,
    Crit                = 6,
    ObjectiveControl    = 7,
    MovementTiles       = 8
}
public enum MinorStatKey
{
    Drill               = 0,
    Skill               = 1,
    Faith               = 2,
    Shock               = 3,
    Crunch              = 4,
    Strength            = 5,
    Voracity            = 6,
    Gumption            = 7,
    Endurance           = 8,
    Robustness          = 9,
    Vitality            = 10,
    Frontage            = 11,
    Impingement         = 12,
    Flank               = 13,
    SoftATK             = 14,
    HardATK             = 15,
    ArmorQuality        = 16,
    Smashthrough        = 17,
    Maneuverability     = 18,
    Adaptability        = 19,
    Luck                = 20,
    Arcane              = 21,
    Pluck               = 22,
    Lethality           = 23,
    Charisma            = 24,
    Jenesaisquoi        = 25,
    Political           = 26,
    Whimsy              = 27,
    Wriggle             = 28,
    Terror              = 29,
    Menace              = 30,
    Limbicity           = 31,
    Benediction         = 32,
    JXIIIVS             = 33
}

public static class StatAbr
{
    public static Dictionary<BaseStatKey, string> Base { get; } = new Dictionary<BaseStatKey, string>
    {
        [BaseStatKey.HP]                = "HP",
        [BaseStatKey.Stamina]           = "STA",
        [BaseStatKey.Priority]          = "PRI",
        [BaseStatKey.Accuracy]          = "ACC",
        [BaseStatKey.Evasion]           = "EVA",
        [BaseStatKey.Defense]           = "DEF",
        [BaseStatKey.Crit]              = "CRI",
        [BaseStatKey.ObjectiveControl]  = "OC",
        [BaseStatKey.MovementTiles]     = "MVT",
    };
    public static Dictionary<MinorStatKey, string> Minor { get; } = new Dictionary<MinorStatKey, string>
    {
        [MinorStatKey.Drill]            = "DRL",
        [MinorStatKey.Skill]            = "SKL",
        [MinorStatKey.Faith]            = "FAI",
        [MinorStatKey.Shock]            = "SHK",
        [MinorStatKey.Crunch]           = "CRN",
        [MinorStatKey.Strength]         = "STR",
        [MinorStatKey.Voracity]         = "VOR",
        [MinorStatKey.Gumption]         = "GUM",
        [MinorStatKey.Endurance]        = "END",
        [MinorStatKey.Robustness]       = "ROB",
        [MinorStatKey.Vitality]         = "VIT",
        [MinorStatKey.Frontage]         = "FRN",
        [MinorStatKey.Impingement]      = "IMP",
        [MinorStatKey.Flank]            = "FLN",
        [MinorStatKey.SoftATK]          = "SAT",
        [MinorStatKey.HardATK]          = "HAT",
        [MinorStatKey.ArmorQuality]     = "ARQ",
        [MinorStatKey.Smashthrough]     = "SMA",
        [MinorStatKey.Maneuverability]  = "MAN",
        [MinorStatKey.Adaptability]     = "ADA",
        [MinorStatKey.Luck]             = "LUK",
        [MinorStatKey.Arcane]           = "ARC",
        [MinorStatKey.Pluck]            = "PLU",
        [MinorStatKey.Lethality]        = "LET",
        [MinorStatKey.Charisma]         = "CHR",
        [MinorStatKey.Jenesaisquoi]     = "JNQ",
        [MinorStatKey.Political]        = "POL",
        [MinorStatKey.Whimsy]           = "WHM",
        [MinorStatKey.Wriggle]          = "WRI",
        [MinorStatKey.Terror]           = "TER",
        [MinorStatKey.Menace]           = "MEN",
        [MinorStatKey.Limbicity]        = "LIM",
        [MinorStatKey.Benediction]      = "BEN",
        [MinorStatKey.JXIIIVS]          = "JXI",
    };
}

public struct StatSheet
{
    public Dictionary<BaseStatKey, Stat> BaseStats { get; set; }
    public Dictionary<MinorStatKey, Stat> MinorStats { get; set; }

    public StatSheet() 
    {
        BaseStats = new Dictionary<BaseStatKey, Stat>();
        MinorStats = new Dictionary<MinorStatKey, Stat>();
    }

    public Stat GetBaseStat(BaseStatKey key)
    {
        if (BaseStats.ContainsKey(key)) return BaseStats[key];
        return new Stat();
    }
    public Stat GetMinorStat(MinorStatKey key)
    {
        if (MinorStats.ContainsKey(key)) return MinorStats[key];
        return new Stat();
    }
}
