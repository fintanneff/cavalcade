using Godot;
using System;

public partial class GenericUnitBuilder : Node3D
{

    [Export] public int TeamIndex { get; set; }
    [Export] public ClassKey SetClass { get; set; }
    [Export] public SpeciesKey SetSpecies { get; set; }

    private PackedScene StartingSpawnScene { get; set; }

    public void BuildUnit()
    {
        StartingSpawnScene = StatRegistry.Inst.Classes[SetClass].DefaultPawnScene;
        GridPawn newGP = StartingSpawnScene.Instantiate<GridPawn>();
        newGP.CharSheet = StatRegistry.Inst.PrebuiltChars["Defaulto"].CloneFromJsonSource();
        newGP.CharSheet.ClassKey = SetClass;
        newGP.CharSheet.SpeciesKey = SetSpecies;
        newGP.TeamIndex = TeamIndex;
        GetParent().AddChild(newGP);
        newGP.GlobalPosition = GlobalPosition;
        QueueFree();
    }
}
