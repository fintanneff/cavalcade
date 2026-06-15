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
        QueueFree();
        if (!Multiplayer.IsServer()) return;
        string charsheet_json_string = StatRegistry.Inst.PrebuiltChars["Defaulto"].ToJSONString();
        Vector2I temp = GridMapGen.Inst.WorldToTilePos(GlobalPosition);
        if (temp.X == -1) temp = new Vector2I(0, 0);
        PawnActionManager.Inst.RpcId(1,"RequestSpawnCountedRPC", temp.X, temp.Y, TeamIndex, charsheet_json_string);
    }
}
