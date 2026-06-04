using Godot;

public partial class JsonDebugTool : Node
{
	public override void _Ready()
	{
        BlankClassJsonToClipboard();
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
}
