using Godot;
using System;
using System.Collections.Generic;

public partial class JsonDebugTool : Node
{

	public override void _Ready()
	{
        BlankEquipmentJsonToClipboard();
	}

	public void BlankSpeciesJsonToClipboard()
	{
        SpeciesSheet speciesSheet = new SpeciesSheet() 
        {
            Name            = "Name",
            SpeciesStats    = new StatSheet()
        };
        speciesSheet.SpeciesStats.BaseStats[BaseStatKey.HP] = new Stat(5, 0, 0);
        speciesSheet.SpeciesStats.MinorStats[MinorStatKey.Gumption] = new Stat(5, 1, 2);
        string jsontext = speciesSheet.ToJSONString();
        DisplayServer.ClipboardSet(jsontext);
    }

    public void BlankClassJsonToClipboard()
    {
        ClassSheet classSheet = new ClassSheet()
        {
            Name                    = "Name",
            ClassStats              = new StatSheet(),
            primaryWeaponType       = WeaponType.Pike,
            secondaryWeaponTypes    = [WeaponType.Sword, WeaponType.Pistol]
        };
        classSheet.ClassStats.BaseStats[BaseStatKey.HP] = new Stat(5, 0, 0);
        classSheet.ClassStats.MinorStats[MinorStatKey.Gumption] = new Stat(5, 1, 2);
        string jsontext = classSheet.ToJSONString();
        DisplayServer.ClipboardSet(jsontext);
    }

    public void BlankCharJsonToClipboard()
    {
        CharSheet charSheet = new CharSheet()
        {
            Name                = "Name",
            Level               = 1,
            SpeciesKey          = SpeciesKey.Jorgan,
            ClassKey            = ClassKey.Dipshit,
            CharStats           = new StatSheet(),
            Wounds              = 0,
            PsycheAttrition     = 0,
            Exp                 = 0,
        };
        string jsontext = charSheet.ToJSONString();
        DisplayServer.ClipboardSet(jsontext);
    }

    public void BlankEquipmentJsonToClipboard()
    {
        Equipment equipment = new Equipment()
        {
            Name = "Stick",
            Type = WeaponType.Sword,
            MaxDurability = 10.0,
            Weight = 1.0,
            Actions = new EquipmentAction[1],
        };
        equipment.Actions[0] = new EquipmentAction()
        {
            Name = "Whack",
            RequiresLoading = false,
            MaxRange = 1,
            MinRange = 0,
            SpecialPattern = Array.Empty<Vector2I>(),
            BaseDamage = 5,
            ScalingStats = [
                MinorStatKey.Whimsy, 
                MinorStatKey.Crunch, 
                MinorStatKey.Strength, 
                MinorStatKey.Skill
                ],
            BaseDurabilityLoss = 0.25,
            BasePriority = 5,
        };
        string jsontext = equipment.ToJSONString();
        DisplayServer.ClipboardSet(jsontext);
    }
}
