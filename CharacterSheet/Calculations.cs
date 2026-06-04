using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;

public class ActionConditions
{
    public CharSheet Actor { get; set; }
    public CharSheet Target { get; set; }
    public int ActorFlankingBonus { get; set; }
    public bool ActorInUnitCohesion { get; set; }
    public bool OtherInUnitCohesion { get; set; }
    public bool IsEnviromentDark { get; set; }
    public bool IsEnviromentLight { get; set; }
}

public partial class Calculations
{

    /*
     * ---------------------------------------
     * Dynamic Outputs (Involving two actors)
     * ---------------------------------------
     */
    int Calc_MinDMG(ActionConditions cond) { return 0; }
    int Calc_PotentialAdditionalDMG(ActionConditions cond) { return 0; }
    int Calc_StaminaLoss(ActionConditions cond) { return 0; }
    int Calc_HitRate(ActionConditions cond) { return 0; }
    int Calc_CritRate(ActionConditions cond) { return 0; }
    int Calc_PsychAttritionGainedOnKill(ActionConditions cond) { return Mathf.CeilToInt(Eff_PsychAttritionOnKillRate(cond.Actor,cond.Target)); }
    float Calc_DurabilityLoss(ActionConditions cond) {
        return Eff_DurabilityLossRate(cond.Actor) + Eff_DurabilityATK(cond.Actor, cond.Target);
    }
    int Calc_TalkActionSuccessRate(ActionConditions cond) { return 0; }
    bool Calc_DoesDouble(ActionConditions cond) {
        return Eff_DoubleValue(cond.Actor, cond.Target) > Eff_DoubleValue(cond.Target, cond.Actor) + 4;
    }
    int Calc_EquipmentFailureRate(ActionConditions cond) { return 0; }

    /*
     * ----------------------------------------------------
     * Visible stats that need conditions but not a target
     * ----------------------------------------------------
     */
    int Calc_Priority(ActionConditions cond) { return 0; }

    /*
     * ---------------------------------------------------------------------------
     * Dynamic Outputs (Stuff that's calculated but doesn't depend on conditions)
     * ---------------------------------------------------------------------------
     */
    int Stat_MaxHP(CharSheet Actor) { return 0; }
    int Stat_MaxStamina(CharSheet Actor) { return 0; }
    int Stat_StaminRegenRate(CharSheet Actor) { return 0; }
    int Stat_Priority(CharSheet Actor) { return 0; }

    /*
     * ---------------------------------------------------------
     * Intermediate Outputs (Usually hidden and used elsewhere)
     * ---------------------------------------------------------
     */
    float Eff_MinDMG(CharSheet subject, CharSheet other) { return 0.0f; }
    float Eff_PotentialAdditionalDMG(CharSheet subject, CharSheet other) { return 0.0f; }
    float Eff_StaminaLossRate(CharSheet subject, CharSheet other) { return 0.0f; }
    float Eff_StaminaLossATK(CharSheet subject, CharSheet other) { return 0.0f; }
    float Eff_DEF(CharSheet subject, CharSheet other) { return 0.0f; }
    float Eff_Crit(CharSheet subject, CharSheet other) { return 0.0f; }
    float Eff_AntiCrit(CharSheet subject, CharSheet other) { return 0.0f; }
    float Eff_Hit(CharSheet subject, CharSheet other) { return 0.0f; }
    float Eff_Evasion(CharSheet subject, CharSheet other) { return 0.0f; }

    // Rate of weapon degradation
    static Dictionary<SpeciesKey, MinorStatKey[]> PsychAttritionOnKillRate_statmap = new Dictionary<SpeciesKey, MinorStatKey[]>
    {
        [SpeciesKey.Shalaket] = [],
        [SpeciesKey.Valencious] = [],
        [SpeciesKey.Fop] = [],
        [SpeciesKey.Jorgan] = [],
    };
    float Eff_PsychAttritionOnKillRate(CharSheet subject, CharSheet other) {
        return 1.0f - (MinorStatCrunch(subject, other, PsychAttritionOnKillRate_statmap)/20); 
    }
    // ---------------------------------------

    // Rate of weapon degradation
    static Dictionary<SpeciesKey, MinorStatKey[]> DurabilityLoss_statmap = new Dictionary<SpeciesKey, MinorStatKey[]>
    {
        [SpeciesKey.Shalaket] = [],
        [SpeciesKey.Valencious] = [],
        [SpeciesKey.Fop] = [],
        [SpeciesKey.Jorgan] = [],
    };
    float Eff_DurabilityLossRate(CharSheet subject) { return MinorStatCrunch(subject, subject, DurabilityLoss_statmap, empty_statmap); }
    // ---------------------------------------

    // Degrading enemy weapon
    static Dictionary<SpeciesKey, MinorStatKey[]> DurabilityAtk_vuln_statmap = new Dictionary<SpeciesKey, MinorStatKey[]>
    {
        [SpeciesKey.Shalaket]   = [],
        [SpeciesKey.Valencious] = [],
        [SpeciesKey.Fop]        = [],
        [SpeciesKey.Jorgan]     = [],
    };
    float Eff_DurabilityATK(CharSheet subject, CharSheet other) { return MinorStatCrunch(subject, other, empty_statmap, DurabilityAtk_vuln_statmap); }
    // ---------------------------------------

    // Value for doubling
    static Dictionary<SpeciesKey, MinorStatKey[]> DoubelValue_statmap = new Dictionary<SpeciesKey, MinorStatKey[]> 
    {
        [SpeciesKey.Shalaket]   = [],
        [SpeciesKey.Valencious] = [],
        [SpeciesKey.Fop]        = [],
        [SpeciesKey.Jorgan]     = [],
    };
    float Eff_DoubleValue(CharSheet subject, CharSheet other) { return MinorStatCrunch(subject, other, DoubelValue_statmap); }
    // ---------------------------------------

    /* 
     * --------------------------------------------
     * General functions which are used repeatedly
     * --------------------------------------------
     */
    float MinorStatCrunch(
        CharSheet subject, 
        CharSheet other, 
        Dictionary<SpeciesKey, MinorStatKey[]> subject_relavant_stats, 
        Dictionary<SpeciesKey, MinorStatKey[]> other_relavant_stats
        )
    {
        MinorStatKey[] stats_considered = 
            [.. subject_relavant_stats[subject.SpeciesKey], 
            .. other_relavant_stats[other.SpeciesKey]];
        stats_considered = stats_considered.Distinct().ToArray();
        if (stats_considered.Length < 1) return 0.0f;
        float total = 0.0f;
        foreach (MinorStatKey statKey in stats_considered)
        {
            total += subject.GetMinorStat(statKey).Value;
        }
        return total /= stats_considered.Length;
    }
    float MinorStatCrunch(
        CharSheet subject,
        CharSheet other,
        Dictionary<SpeciesKey, MinorStatKey[]> relavant_stats
        )
    {
        return MinorStatCrunch(subject, other, relavant_stats, relavant_stats);
    }
    static Dictionary<SpeciesKey, MinorStatKey[]> empty_statmap = new Dictionary<SpeciesKey, MinorStatKey[]>();
}
